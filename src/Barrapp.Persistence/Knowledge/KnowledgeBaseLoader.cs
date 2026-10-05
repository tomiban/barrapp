using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Barrapp.Domain.Knowledge;
using Microsoft.Extensions.Logging;

namespace Barrapp.Persistence.Knowledge;

/// <summary>
/// Lee, deserializa y valida los tres JSON de la base de conocimiento. La validación semántica la
/// delega en <see cref="KnowledgeBase.Create"/>; aquí se comprueba el sobre del fichero
/// (<c>schemaVersion</c>) y se traduce cualquier fallo a <see cref="KnowledgeBaseValidationException"/>
/// con log de nivel <c>Critical</c>, para que el arranque falle rápido y con un mensaje claro.
/// </summary>
internal sealed class KnowledgeBaseLoader(ILogger<KnowledgeBaseLoader> logger)
{
    /// <summary>Versión de esquema que entiende este cargador.</summary>
    internal const int SupportedSchemaVersion = 1;

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    /// <summary>Carga el catálogo desde los recursos embebidos del proyecto de persistencia.</summary>
    public KnowledgeBase LoadEmbeddedResources()
    {
        var assembly = typeof(KnowledgeBaseLoader).Assembly;

        return Load(
            ReadResource(assembly, KnowledgeFiles.Exercises),
            ReadResource(assembly, KnowledgeFiles.Skills),
            ReadResource(assembly, KnowledgeFiles.Routines));
    }

    /// <summary>
    /// Deserializa los tres documentos y devuelve el catálogo validado. Lanza
    /// <see cref="KnowledgeBaseValidationException"/> si alguno no cumple el esquema.
    /// </summary>
    public KnowledgeBase Load(string exercisesJson, string skillsJson, string routinesJson)
    {
        var exercises = Deserialize<ExercisesFile>(exercisesJson, KnowledgeFiles.Exercises).Exercises;
        var skills = Deserialize<SkillsFile>(skillsJson, KnowledgeFiles.Skills).Skills;
        var programs = Deserialize<RoutinesFile>(routinesJson, KnowledgeFiles.Routines).Programs;

        var result = KnowledgeBase.Create(exercises, skills, programs);
        if (result.IsFailure)
        {
            throw Fail(result.Error.Description, result.Error.Code);
        }

        return result.Value;
    }

    private TFile Deserialize<TFile>(string json, string fileName)
        where TFile : KnowledgeFile
    {
        TFile? file;
        try
        {
            file = JsonSerializer.Deserialize<TFile>(json, SerializerOptions);
        }
        catch (JsonException exception)
        {
            throw Fail(
                $"El fichero '{fileName}' no es un JSON válido: {exception.Message}",
                "knowledge.invalid_json");
        }

        if (file is null)
        {
            throw Fail($"El fichero '{fileName}' está vacío.", "knowledge.empty_file");
        }

        if (file.SchemaVersion != SupportedSchemaVersion)
        {
            throw Fail(
                $"El fichero '{fileName}' usa schemaVersion {file.SchemaVersion}; se esperaba {SupportedSchemaVersion}.",
                "knowledge.unsupported_schema_version");
        }

        return file;
    }

    private static string ReadResource(Assembly assembly, string resourceName)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"No se encontró el recurso embebido '{resourceName}'. Revisa Barrapp.Persistence.csproj.");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private KnowledgeBaseValidationException Fail(string message, string code)
    {
        logger.LogCritical("Base de conocimiento inválida ({Code}): {Message}", code, message);
        return new KnowledgeBaseValidationException(message, code);
    }
}
