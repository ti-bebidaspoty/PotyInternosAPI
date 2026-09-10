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

    public virtual DbSet<AplicacaoCampoAdicional> AplicacoesCamposAdicionais { get; set; } = null!;

    public virtual DbSet<UsuariosAplicacao> UsuariosAplicacoes { get; set; } = null!;

    public virtual DbSet<UsuarioAplicacaoCampoAdicionalValor> UsuariosAplicacoesCamposAdicionaisValores { get; set; } = null!;

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

        modelBuilder.Entity<AplicacaoCampoAdicional>(entity =>
        {
            entity.ToTable("AplicacoesCamposAdicionais", "Global");

            entity.HasKey(e => e.CampoAdicionalId)
                .HasName("PK_AplicacoesCamposAdicionais");

            entity.Property(e => e.CampoAdicionalId)
                .HasColumnName("CampoAdicionalID")
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.Property(e => e.AplicacaoId)
                .HasColumnName("AplicacaoID")
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.Property(e => e.Nome)
                .HasColumnName("Campo")
                .HasMaxLength(200)
                .IsUnicode(false);

            entity.Property(e => e.Tipo)
                .HasColumnName("Tipo")
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.Property(e => e.Ordem)
                .HasColumnName("Ordem")
                .HasDefaultValue(0);

            entity.HasOne(e => e.Aplicacao)
                .WithMany(a => a.CamposAdicionais)
                .HasForeignKey(e => e.AplicacaoId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_AplicacoesCamposAdicionais_Aplicacoes");

            entity.HasIndex(e => new { e.AplicacaoId, e.Nome })
                .IsUnique()
                .HasDatabaseName("UX_AplicacoesCamposAdicionais_Aplicacao_Campo");
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

        modelBuilder.Entity<UsuarioAplicacaoCampoAdicionalValor>(entity =>
        {
            entity.ToTable("UsuariosAplicacoesCamposAdicionaisValores", "Global");

            entity.HasKey(e => new { e.UsuarioId, e.AplicacaoId, e.CampoAdicionalId })
                .HasName("PK_UsuariosAplicacoesCamposAdicionaisValores");

            entity.Property(e => e.UsuarioId)
                .HasColumnName("UsuarioID")
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.Property(e => e.AplicacaoId)
                .HasColumnName("AplicacaoID")
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.Property(e => e.CampoAdicionalId)
                .HasColumnName("CampoAdicionalID")
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.Property(e => e.Valor)
                .HasColumnName("Valor")
                .HasMaxLength(500)
                .IsUnicode(false);

            entity.HasOne(e => e.UsuarioAplicacao)
                .WithMany(ua => ua.CamposAdicionaisValores)
                .HasForeignKey(e => new { e.UsuarioId, e.AplicacaoId })
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_UsuariosAplicacoesCamposValores_UsuariosAplicacoes");

            entity.HasOne(e => e.CampoAdicional)
                .WithMany(c => c.Valores)
                .HasForeignKey(e => e.CampoAdicionalId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_UsuariosAplicacoesCamposValores_Campos");
        });
    }
}
