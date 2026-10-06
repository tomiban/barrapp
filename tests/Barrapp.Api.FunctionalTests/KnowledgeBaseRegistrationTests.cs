using Barrapp.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Barrapp.Api.FunctionalTests;

/// <summary>
/// La composición registra el catálogo de conocimiento como singleton y lo carga al arrancar
/// (fail-fast). Aquí se comprueba la parte de DI; el contenido lo cubre el cargador.
/// </summary>
public sealed class KnowledgeBaseRegistrationTests(BarrappApiFactory factory)
    : IClassFixture<BarrappApiFactory>
{
    [Fact]
    public void Knowledge_base_is_registered_as_a_singleton()
    {
        var first = factory.Services.GetRequiredService<IKnowledgeBase>();
        var second = factory.Services.GetRequiredService<IKnowledgeBase>();

        Assert.Same(first, second);
    }

    [Fact]
    public void Knowledge_base_catalog_is_loaded_and_queryable()
    {
        var catalog = factory.Services.GetRequiredService<IKnowledgeBase>();

        Assert.NotNull(catalog.Exercises);
        Assert.NotNull(catalog.Skills);
        Assert.NotNull(catalog.Programs);
    }
}
