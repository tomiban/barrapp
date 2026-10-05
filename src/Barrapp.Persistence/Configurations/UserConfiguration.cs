using Barrapp.Application.Common;
using Barrapp.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Barrapp.Persistence.Configurations;

/// <summary>
/// Mapeo EF Core del usuario. En el MVP mono-usuario y sin login se siembra una única fila con
/// el identificador de <see cref="SingleUser"/>; la creación real de usuarios llega con auth.
/// </summary>
internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(user => user.Id);

        builder.HasData(User.Create(SingleUser.Id));
    }
}
