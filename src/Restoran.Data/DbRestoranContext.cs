using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Restoran.Data.Entities;

namespace Restoran.Data;

public partial class DbRestoranContext : DbContext
{
    public DbRestoranContext()
    {
    }

    public DbRestoranContext(DbContextOptions<DbRestoranContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Adres> Adres { get; set; }

    public virtual DbSet<Iade> Iades { get; set; }

    public virtual DbSet<Kasa> Kasas { get; set; }

    public virtual DbSet<Kategori> Kategoris { get; set; }

    public virtual DbSet<Malzemeler> Malzemelers { get; set; }

    public virtual DbSet<Masa> Masas { get; set; }

    public virtual DbSet<Odeme> Odemes { get; set; }

    public virtual DbSet<Personel> Personels { get; set; }

    public virtual DbSet<PersonelIzin> PersonelIzins { get; set; }

    public virtual DbSet<Rezervasyon> Rezervasyons { get; set; }

    public virtual DbSet<Roller> Rollers { get; set; }

    public virtual DbSet<SiparisDetay> SiparisDetays { get; set; }

    public virtual DbSet<Siparisler> Siparislers { get; set; }

    public virtual DbSet<StokHareket> StokHarekets { get; set; }

    public virtual DbSet<UrunRecetesi> UrunRecetesis { get; set; }

    public virtual DbSet<Urunler> Urunlers { get; set; }

    public virtual DbSet<Uyeler> Uyelers { get; set; }

    public virtual DbSet<MalzemeTalep> MalzemeTalepleri { get; set; }

    // 🔥 YENİ: Personel İzin Geçmişi View'i
    public virtual DbSet<vw_PersonelIzin_Gecmisi> vw_PersonelIzin_Gecmisi { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 🔥 View'i tanımla (önce tanımla, sonra diğer entity'ler)
        modelBuilder.Entity<vw_PersonelIzin_Gecmisi>(entity =>
        {
            entity.HasNoKey(); // View'in primary key'i yok
            entity.ToView("vw_PersonelIzin_Gecmisi"); // View adı
        });

        modelBuilder.Entity<Adres>(entity =>
        {
            entity.HasKey(e => e.AdresId).HasName("PK__Adres__DA8DEA6C0FC10825");

            entity.Property(e => e.AdresId).HasColumnName("AdresID");
            entity.Property(e => e.AdresTipi).HasMaxLength(20);
            entity.Property(e => e.TeslimatBolgesindeMi).HasDefaultValue(false);
            entity.Property(e => e.UyeId).HasColumnName("UyeID");

            entity.HasOne(d => d.Uye).WithMany(p => p.Adres)
                .HasForeignKey(d => d.UyeId)
                .HasConstraintName("FK_Adres_Uyeler");
        });

        modelBuilder.Entity<Iade>(entity =>
        {
            entity.HasKey(e => e.IadeId).HasName("PK__Iade__D047997FB4226E2E");

            entity.ToTable("Iade");

            entity.Property(e => e.IadeId).HasColumnName("IadeID");
            entity.Property(e => e.IadeDurumu)
                .HasMaxLength(20)
                .HasDefaultValue("BEKLEMEDE");
            entity.Property(e => e.IadeTarihi)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IadeTutari).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.PersonelId).HasColumnName("PersonelID");
            entity.Property(e => e.SiparisDetayId).HasColumnName("SiparisDetayID");
            entity.Property(e => e.UrunId).HasColumnName("UrunID");

            entity.HasOne(d => d.Personel).WithMany(p => p.Iades)
                .HasForeignKey(d => d.PersonelId)
                .HasConstraintName("FK_Iade_Personel");

            entity.HasOne(d => d.SiparisDetay).WithMany(p => p.Iades)
                .HasForeignKey(d => d.SiparisDetayId)
                .HasConstraintName("FK_Iade_SiparisDetay");

            entity.HasOne(d => d.Urun).WithMany(p => p.Iades)
                .HasForeignKey(d => d.UrunId)
                .HasConstraintName("FK_Iade_Urunler");
        });

        modelBuilder.Entity<Kasa>(entity =>
        {
            entity.HasKey(e => e.KasaId).HasName("PK__Kasa__1595E41150A253EE");

            entity.ToTable("Kasa");

            entity.Property(e => e.KasaId).HasColumnName("KasaID");
            entity.Property(e => e.AcilisTarihi).HasColumnType("datetime");
            entity.Property(e => e.KapanisTarihi).HasColumnType("datetime");
            entity.Property(e => e.KasaDurumu).HasMaxLength(20);
            entity.Property(e => e.AcilisBakiyesi).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.KapanisBakiyesi).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.PersonelId).HasColumnName("PersonelID");


            entity.HasOne(d => d.Personel).WithMany(p => p.Kasas)
                .HasForeignKey(d => d.PersonelId)
                .HasConstraintName("FK_Kasa_Personel");
        });

        modelBuilder.Entity<Kategori>(entity =>
        {
            entity.HasKey(e => e.KategoriId).HasName("PK__Kategori__1782CC926A0AF70B");

            entity.ToTable("Kategori");

            entity.Property(e => e.KategoriId).HasColumnName("KategoriID");
            entity.Property(e => e.KategoriAdi).HasMaxLength(50);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<Malzemeler>(entity =>
        {
            entity.HasKey(e => e.MalzemeId).HasName("PK__Malzemel__4ED155E09E404CB7");

            entity.ToTable("Malzemeler");

            entity.Property(e => e.MalzemeId).HasColumnName("MalzemeID");
            entity.Property(e => e.Birim).HasMaxLength(20);
            entity.Property(e => e.BirimMaliyeti).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.MalzemeAdi).HasMaxLength(100);
            entity.Property(e => e.StokMiktari).HasColumnType("decimal(10, 2)");
        });

        modelBuilder.Entity<Masa>(entity =>
        {
            entity.HasKey(e => e.MasaId).HasName("PK__Masa__9F94EBD36A10F83C");

            entity.ToTable("Masa");

            entity.Property(e => e.MasaId).HasColumnName("MasaID");
            entity.Property(e => e.MasaDurumu).HasMaxLength(20);
            entity.Property(e => e.MasaNo).HasMaxLength(20);
        });

        modelBuilder.Entity<Odeme>(entity =>
        {
            entity.HasKey(e => e.OdemeId).HasName("PK__Odeme__B11B66AD0AE797C9");

            entity.ToTable("Odeme");

            entity.Property(e => e.OdemeId).HasColumnName("OdemeID");
            entity.Property(e => e.KasaId).HasColumnName("KasaID");
            entity.Property(e => e.OdemeTarihi)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.OdemeTipi).HasMaxLength(20);
            entity.Property(e => e.OdemeTutari).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.PersonelId).HasColumnName("PersonelID");
            entity.Property(e => e.SiparisId).HasColumnName("SiparisID");

            entity.HasOne(d => d.Kasa).WithMany(p => p.Odemes)
                .HasForeignKey(d => d.KasaId)
                .HasConstraintName("FK_Odeme_Kasa");

            entity.HasOne(d => d.Personel).WithMany(p => p.Odemes)
                .HasForeignKey(d => d.PersonelId)
                .HasConstraintName("FK_Odeme_Personel");

            entity.HasOne(d => d.Siparis).WithMany(p => p.Odemes)
                .HasForeignKey(d => d.SiparisId)
                .HasConstraintName("FK_Odeme_Siparisler");
        });

        modelBuilder.Entity<Personel>(entity =>
        {
            entity.HasKey(e => e.PersonelId).HasName("PK__Personel__0F0C5751E6FDDC9A");

            entity.ToTable("Personel");

            entity.Property(e => e.PersonelId).HasColumnName("PersonelID");
            entity.Property(e => e.Cinsiyet).HasMaxLength(10);
            entity.Property(e => e.IseBaslamaTarihi).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.KullaniciAdi).HasMaxLength(50);
            entity.Property(e => e.Maas).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.PersonelAdi).HasMaxLength(50);
            entity.Property(e => e.PersonelSifre).HasMaxLength(255);
            entity.Property(e => e.PersonelSoyadi).HasMaxLength(50);
            entity.Property(e => e.PersonelTelefon).HasMaxLength(15);
            entity.Property(e => e.RolId).HasColumnName("RolID");
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            // 🆕 VARDİYA ALANLARI EKLENDİ
            entity.Property(e => e.VardiyaBaslangic).HasMaxLength(10).HasDefaultValue("09:00");
            entity.Property(e => e.VardiyaBitis).HasMaxLength(10).HasDefaultValue("18:00");
            entity.Property(e => e.CalismaGunleri).HasMaxLength(20).HasDefaultValue("1,2,3,4,5");
            entity.Property(e => e.VardiyaAktifMi).HasDefaultValue(true);

            entity.HasOne(d => d.Rol).WithMany(p => p.Personels)
                .HasForeignKey(d => d.RolId)
                .HasConstraintName("FK_Personel_Roller");
        });

        modelBuilder.Entity<PersonelIzin>(entity =>
        {
            entity.HasKey(e => e.IzinId).HasName("PK__Personel__4700791E92F85897");

            entity.ToTable("PersonelIzin");

            entity.Property(e => e.IzinId).HasColumnName("IzinID");
            entity.Property(e => e.IzinDurumu)
                .HasMaxLength(20)
                .HasDefaultValue("BEKLEMEDE");
            entity.Property(e => e.PersonelId).HasColumnName("PersonelID");

            entity.HasOne(d => d.Personel).WithMany(p => p.PersonelIzins)
                .HasForeignKey(d => d.PersonelId)
                .HasConstraintName("FK_PersonelIzin_Personel");
        });

        modelBuilder.Entity<Rezervasyon>(entity =>
        {
            entity.HasKey(e => e.RezervasyonId).HasName("PK__Rezervas__CD4DF9786785B85B");

            entity.ToTable("Rezervasyon");

            entity.Property(e => e.RezervasyonId).HasColumnName("RezervasyonID");
            entity.Property(e => e.Durum)
                .HasMaxLength(20)
                .HasDefaultValue("BEKLEMEDE");
            entity.Property(e => e.MasaId).HasColumnName("MasaID");
            entity.Property(e => e.MusteriAdi).HasMaxLength(20);
            entity.Property(e => e.MusteriSoyadi).HasMaxLength(20);
            entity.Property(e => e.OlusturulmaTarihi)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.RezervasyonTipi).HasMaxLength(20);
            entity.Property(e => e.TarihSaat).HasColumnType("datetime");
            entity.Property(e => e.Telefon).HasMaxLength(15);
            entity.Property(e => e.UyeId).HasColumnName("UyeID");

            entity.HasOne(d => d.Masa).WithMany(p => p.Rezervasyons)
                .HasForeignKey(d => d.MasaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Rezervasyon_Masa");

            entity.HasOne(d => d.Uye).WithMany()
                .HasForeignKey(d => d.UyeId)
                .HasConstraintName("FK_Rezervasyon_Uyeler");
        });

        modelBuilder.Entity<Roller>(entity =>
        {
            entity.HasKey(e => e.RolId).HasName("PK__Roller__F92302D1B3A8BA87");

            entity.ToTable("Roller");

            entity.Property(e => e.RolId).HasColumnName("RolID");
            entity.Property(e => e.RolAdi).HasMaxLength(50);
            entity.Property(e => e.RolDurumu).HasDefaultValue(true);
        });

        modelBuilder.Entity<SiparisDetay>(entity =>
        {
            entity.HasKey(e => e.SiparisDetayId).HasName("PK__SiparisD__DA4BD83290A9C705");

            entity.ToTable("SiparisDetay");

            entity.Property(e => e.SiparisDetayId).HasColumnName("SiparisDetayID");
            entity.Property(e => e.BirimFiyat).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.SiparisId).HasColumnName("SiparisID");
            entity.Property(e => e.UrunId).HasColumnName("UrunID");

            entity.HasOne(d => d.Siparis).WithMany(p => p.SiparisDetays)
                .HasForeignKey(d => d.SiparisId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SiparisDetay_Siparisler");

            entity.HasOne(d => d.Urun).WithMany(p => p.SiparisDetays)
                .HasForeignKey(d => d.UrunId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SiparisDetay_Urunler");
        });

        modelBuilder.Entity<Siparisler>(entity =>
        {
            entity.HasKey(e => e.SiparisId).HasName("PK__Siparisl__C3F03BDD36366D4C");

            entity.ToTable("Siparisler");

            entity.Property(e => e.SiparisId).HasColumnName("SiparisID");
            entity.Property(e => e.MasaId).HasColumnName("MasaID");
            entity.Property(e => e.PersonelId).HasColumnName("PersonelID");
            entity.Property(e => e.SiparisDurumu).HasMaxLength(20);
            entity.Property(e => e.SiparisTarihi)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.SiparisTipi).HasMaxLength(20);
            entity.Property(e => e.ToplamTutar)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(10, 2)");
            entity.Property(e => e.UyeId).HasColumnName("UyeID");

            entity.HasOne(d => d.Masa).WithMany(p => p.Siparislers)
                .HasForeignKey(d => d.MasaId)
                .HasConstraintName("FK_Siparisler_Masa");

            entity.HasOne(d => d.Personel).WithMany(p => p.Siparislers)
                .HasForeignKey(d => d.PersonelId)
                .HasConstraintName("FK_Siparisler_Personel");

            entity.HasOne(d => d.Uye).WithMany(p => p.Siparislers)
                .HasForeignKey(d => d.UyeId)
                .HasConstraintName("FK_Siparisler_Uyeler");
        });

        modelBuilder.Entity<StokHareket>(entity =>
        {
            entity.HasKey(e => e.StokHareketId).HasName("PK__StokHare__8F9B28C09F5FBF46");

            entity.ToTable("StokHareket");

            entity.Property(e => e.StokHareketId).HasColumnName("StokHareketID");
            entity.Property(e => e.IsleminTarihSaati)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.PersonelId).HasColumnName("PersonelID");
            entity.Property(e => e.StokIslemTipi).HasMaxLength(20);
            entity.Property(e => e.UrunId).HasColumnName("UrunID");

            entity.HasOne(d => d.Personel).WithMany(p => p.StokHarekets)
                .HasForeignKey(d => d.PersonelId)
                .HasConstraintName("FK_StokHareket_Personel");

            entity.HasOne(d => d.Urun).WithMany(p => p.StokHarekets)
                .HasForeignKey(d => d.UrunId)
                .HasConstraintName("FK_StokHareket_Urunler");
        });

        modelBuilder.Entity<UrunRecetesi>(entity =>
        {
            entity.HasKey(e => e.ReceteId).HasName("PK__Urun_Rec__02D0477B61F5DE92");

            entity.ToTable("Urun_Recetesi");

            entity.Property(e => e.ReceteId).HasColumnName("ReceteID");
            entity.Property(e => e.KullanimMiktari).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.MalzemeId).HasColumnName("MalzemeID");
            entity.Property(e => e.UrunId).HasColumnName("UrunID");

            entity.HasOne(d => d.Malzeme).WithMany(p => p.UrunRecetesis)
                .HasForeignKey(d => d.MalzemeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UrunRecetesi_Malzemeler");

            entity.HasOne(d => d.Urun).WithMany(p => p.UrunRecetesis)
                .HasForeignKey(d => d.UrunId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UrunRecetesi_Urunler");
        });

        modelBuilder.Entity<Urunler>(entity =>
        {
            entity.HasKey(e => e.UrunId).HasName("PK__Urunler__623D364BF036AED3");

            entity.ToTable("Urunler");

            entity.Property(e => e.UrunId).HasColumnName("UrunID");
            entity.Property(e => e.Fiyat).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.KategoriId).HasColumnName("KategoriID");
            entity.Property(e => e.StokMiktari).HasDefaultValue(0);
            entity.Property(e => e.UrunAdi).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.Kategori).WithMany(p => p.Urunlers)
                .HasForeignKey(d => d.KategoriId)
                .HasConstraintName("FK_Urunler_Kategori");
        });

        modelBuilder.Entity<Uyeler>(entity =>
        {
            entity.HasKey(e => e.UyeId).HasName("PK__Uyeler__76F7D9EFEB6A17E3");

            entity.ToTable("Uyeler");

            entity.Property(e => e.UyeId).HasColumnName("UyeID");
            entity.Property(e => e.Cinsiyet).HasMaxLength(10);
            entity.Property(e => e.KayitTarihi)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.UyeAdi).HasMaxLength(50);
            entity.Property(e => e.UyeEmail).HasMaxLength(100);
            entity.Property(e => e.UyeSifre).HasMaxLength(255);
            entity.Property(e => e.UyeSoyadi).HasMaxLength(50);
            entity.Property(e => e.UyeTelefon).HasMaxLength(15);
        });

        // 🆕 BİLDİRİM TABLOSU MODEL YAPILANDIRMASI
        modelBuilder.Entity<Bildirim>(entity =>
        {
            entity.HasKey(e => e.BildirimId).HasName("PK__Bildirim__BildirimID");

            entity.ToTable("Bildirim");

            entity.Property(e => e.BildirimId).HasColumnName("BildirimID");
            entity.Property(e => e.KullaniciId).HasColumnName("KullaniciID");
            entity.Property(e => e.Baslik).HasMaxLength(200);
            entity.Property(e => e.Mesaj).HasMaxLength(500);
            entity.Property(e => e.OlusturmaTarihi)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.OkunduMu).HasDefaultValue(false);
            entity.Property(e => e.Tip).HasMaxLength(50);

            entity.HasOne(d => d.Kullanici)
                .WithMany()
                .HasForeignKey(d => d.KullaniciId)
                .HasConstraintName("FK_Bildirim_Uyeler")
                .OnDelete(DeleteBehavior.SetNull);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}