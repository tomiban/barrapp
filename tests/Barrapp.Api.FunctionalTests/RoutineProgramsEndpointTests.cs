using System.Net;
using System.Net.Http.Json;
using Barrapp.Application.Features.Catalog;

namespace Barrapp.Api.FunctionalTests;

/// <summary>
/// Los programas generales de acondicionamiento se sirven con sus rutinas y bloques, con el tipo
/// en minúsculas (<c>circuit</c>/<c>strength</c>) y las filas con su rango y su descanso.
/// </summary>
public sealed class RoutineProgramsEndpointTests(BarrappApiFactory factory)
    : IClassFixture<BarrappApiFactory>
{
    [Fact]
    public async Task Get_routines_returns_the_three_programs_with_their_routines()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/catalog/routines");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var programs = await response.Content.ReadFromJsonAsync<List<RoutineProgramResponse>>();
        Assert.NotNull(programs);
        Assert.Equal(
            ["ponte-en-forma", "base-perfecta", "home-workout"],
            programs!.Select(program => program.Id));

        var ponteEnForma = programs.Single(program => program.Id == "ponte-en-forma");
        Assert.Equal("Ponte en forma", ponteEnForma.Name);
        Assert.Equal("circuit", ponteEnForma.Type);
        Assert.False(string.IsNullOrWhiteSpace(ponteEnForma.Description));
        Assert.Equal(5, ponteEnForma.Routines.Count);

        var basePerfecta = programs.Single(program => program.Id == "base-perfecta");
        Assert.Equal("Base Perfecta", basePerfecta.Name);
        Assert.Equal("strength", basePerfecta.Type);
        Assert.Equal(8, basePerfecta.Routines.Count);

        Assert.Equal("strength", programs.Single(program => program.Id == "home-workout").Type);
        Assert.Equal(5, programs.Single(program => program.Id == "home-workout").Routines.Count);
    }

    [Fact]
    public async Task Get_routines_includes_blocks_and_items_with_their_ranges_and_rests()
    {
        using var client = factory.CreateClient();

        var programs = await client.GetFromJsonAsync<List<RoutineProgramResponse>>("/catalog/routines");

        Assert.NotNull(programs);

        var first = programs!.Single(program => program.Id == "ponte-en-forma").Routines[0];
        Assert.Equal("ponte-en-forma-r1", first.Id);
        Assert.Equal("Rutina 1", first.Name);
        Assert.Equal(2, first.Intensity);
        Assert.Equal(14, first.DurationMinutes);
        Assert.NotEmpty(first.Blocks);

        var block = first.Blocks[0];
        Assert.Equal("SET 1", block.Name);
        Assert.Equal(3, block.Rounds);
        Assert.Equal(0, block.RestSeconds);
        Assert.NotEmpty(block.Items);

        var hold = block.Items[0];
        Assert.Equal("shoulder-tap-plank", hold.ExerciseId);
        Assert.Equal(1, hold.Sets);
        Assert.Equal(15, hold.HoldSecondsMin);
        Assert.Equal(15, hold.HoldSecondsMax);
        Assert.Null(hold.RepsMin);
        Assert.Null(hold.RepsMax);
        Assert.Equal(0, hold.RestSeconds);

        // Los programas de fuerza usan repeticiones y superseries en sus filas.
        var strength = programs.Single(program => program.Id == "home-workout");
        var strengthItems = strength.Routines
            .SelectMany(routine => routine.Blocks)
            .SelectMany(blockItems => blockItems.Items)
            .ToList();
        Assert.Contains(strengthItems, item => item.RepsMin is not null && item.RepsMax is not null);
        Assert.Contains(strengthItems, item => item.SupersetGroup is not null);
    }
}
