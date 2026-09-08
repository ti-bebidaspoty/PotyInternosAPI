using Microsoft.EntityFrameworkCore;
using PotyInternosAPI.Models;

namespace PotyInternosAPI.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Departamento> Departamentos { get; set; } = null!;

    public virtual DbSet<Usuario> Usuarios { get; set; } = null!;

    public virtual DbSet<Aplicacao> Aplicacoes { get; set; } = null!;

    public virtual DbSet<UsuariosAplicacao> UsuariosAplicacoes { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Departamento>(entity =>
        {
            entity.ToTable("Departamentos", "Global");

            entity.HasKey(e => e.DepartamentoId);

            entity.Property(e => e.DepartamentoId)
                .HasColumnName("DepartamentoID")
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.Property(e => e.Nome)
                .HasColumnName("Departamento")
                .HasMaxLength(200)
                .IsUnicode(false);

            entity.Property(e => e.Status)
                .HasColumnName("Status")
                .HasDefaultValue(true);
        });

        modelBuilder.Entity<Aplicacao>(entity =>
        {
            entity.ToTable("Aplicacoes", "Global");

            entity.HasKey(e => e.AplicacaoId);

            entity.Property(e => e.AplicacaoId)
                .HasColumnName("AplicacaoID")
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.Property(e => e.Nome)
                .HasColumnName("Aplicacao")
                .HasMaxLength(200)
                .IsUnicode(false);

            entity.Property(e => e.Status)
                .HasColumnName("Status")
                .HasDefaultValue(true);
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("Usuarios", "Global");

            entity.HasKey(e => e.UsuarioId);

            entity.Property(e => e.UsuarioId)
                .HasColumnName("UsuarioID")
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.Property(e => e.Nome)
                .HasColumnName("Nome")
                .HasMaxLength(200)
                .IsUnicode(false);

            entity.Property(e => e.NomeUsuario)
                .HasColumnName("Usuarios")
                .HasMaxLength(200)
                .IsUnicode(false);

            entity.Property(e => e.Senha)
                .HasColumnName("Senha")
                .HasMaxLength(200)
                .IsUnicode(false);

            entity.Property(e => e.DepartamentoId)
                .HasColumnName("DepartamentoID")
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.Property(e => e.Status)
                .HasColumnName("Status")
                .HasDefaultValue(true);

            entity.Property(e => e.IsAdmin)
                .HasColumnName("IsAdmin")
                .HasDefaultValue(false);

            entity.HasOne(e => e.Departamento)
                .WithMany(d => d.Usuarios)
                .HasForeignKey(e => e.DepartamentoId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Usuarios_Departamentos");
        });

        modelBuilder.Entity<UsuariosAplicacao>(entity =>
        {
            entity.ToTable("UsuariosAplicacoes", "Global");

            entity.HasKey(e => new { e.UsuarioId, e.AplicacaoId })
                .HasName("PK_UsuariosAplicacoes");

            entity.Property(e => e.UsuarioId)
                .HasColumnName("UsuarioID")
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.Property(e => e.AplicacaoId)
                .HasColumnName("AplicacaoID")
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(e => e.Usuario)
                .WithMany(u => u.UsuariosAplicacoes)
                .HasForeignKey(e => e.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_UsuariosAplicacoes_Usuarios");

            entity.HasOne(e => e.Aplicacao)
                .WithMany(a => a.UsuariosAplicacoes)
                .HasForeignKey(e => e.AplicacaoId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_UsuariosAplicacoes_Aplicacoes");
        });
    }
}
