using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Restoran.Data.Migrations
{
    /// <inheritdoc />
    public partial class MalzemeTalepEkle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Kategori",
                columns: table => new
                {
                    KategoriID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsActive = table.Column<bool>(type: "bit", nullable: true, defaultValue: true),
                    SilinmeTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    KategoriAdi = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Kategori__1782CC926A0AF70B", x => x.KategoriID);
                });

            migrationBuilder.CreateTable(
                name: "Malzemeler",
                columns: table => new
                {
                    MalzemeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MalzemeAdi = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StokMiktari = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Birim = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    BirimMaliyeti = table.Column<decimal>(type: "decimal(10,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Malzemel__4ED155E09E404CB7", x => x.MalzemeID);
                });

            migrationBuilder.CreateTable(
                name: "Masa",
                columns: table => new
                {
                    MasaID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MasaNo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    MasaDurumu = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Masa__9F94EBD36A10F83C", x => x.MasaID);
                });

            migrationBuilder.CreateTable(
                name: "Roller",
                columns: table => new
                {
                    RolID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RolAdi = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RolDurumu = table.Column<bool>(type: "bit", nullable: true, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Roller__F92302D1B3A8BA87", x => x.RolID);
                });

            migrationBuilder.CreateTable(
                name: "Uyeler",
                columns: table => new
                {
                    UyeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UyeAdi = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UyeSoyadi = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UyeTelefon = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    UyeEmail = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UyeSifre = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Cinsiyet = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    KayitTarihi = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    IsActive = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Uyeler__76F7D9EFEB6A17E3", x => x.UyeID);
                });

            migrationBuilder.CreateTable(
                name: "Urunler",
                columns: table => new
                {
                    UrunID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UrunAdi = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Fiyat = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    StokMiktari = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    Aciklamalar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    KategoriID = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true, defaultValue: true),
                    SilinmeTarihi = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Urunler__623D364BF036AED3", x => x.UrunID);
                    table.ForeignKey(
                        name: "FK_Urunler_Kategori",
                        column: x => x.KategoriID,
                        principalTable: "Kategori",
                        principalColumn: "KategoriID");
                });

            migrationBuilder.CreateTable(
                name: "MalzemeTalepleri",
                columns: table => new
                {
                    TalepId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MalzemeId = table.Column<int>(type: "int", nullable: false),
                    Miktar = table.Column<int>(type: "int", nullable: false),
                    Birim = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TalepEden = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PersonelId = table.Column<int>(type: "int", nullable: true),
                    Durum = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TalepTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CevaplamaTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Cevaplayan = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MalzemeTalepleri", x => x.TalepId);
                    table.ForeignKey(
                        name: "FK_MalzemeTalepleri_Malzemeler_MalzemeId",
                        column: x => x.MalzemeId,
                        principalTable: "Malzemeler",
                        principalColumn: "MalzemeID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Personel",
                columns: table => new
                {
                    PersonelID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PersonelAdi = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RefreshToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RefreshTokenBitis = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PersonelSoyadi = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    KullaniciAdi = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PersonelSifre = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PersonelTelefon = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    Cinsiyet = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    IseBaslamaTarihi = table.Column<DateOnly>(type: "date", nullable: true, defaultValueSql: "(getdate())"),
                    Maas = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    RolID = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true, defaultValue: true),
                    SilinmeTarihi = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Personel__0F0C5751E6FDDC9A", x => x.PersonelID);
                    table.ForeignKey(
                        name: "FK_Personel_Roller",
                        column: x => x.RolID,
                        principalTable: "Roller",
                        principalColumn: "RolID");
                });

            migrationBuilder.CreateTable(
                name: "Adres",
                columns: table => new
                {
                    AdresID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdresTipi = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AcikAdres = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TeslimatBolgesindeMi = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    UyeID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Adres__DA8DEA6C0FC10825", x => x.AdresID);
                    table.ForeignKey(
                        name: "FK_Adres_Uyeler",
                        column: x => x.UyeID,
                        principalTable: "Uyeler",
                        principalColumn: "UyeID");
                });

            migrationBuilder.CreateTable(
                name: "Rezervasyon",
                columns: table => new
                {
                    RezervasyonID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MusteriAdi = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    MusteriSoyadi = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Telefon = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    KisiSayisi = table.Column<int>(type: "int", nullable: false),
                    TarihSaat = table.Column<DateTime>(type: "datetime", nullable: false),
                    Durum = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, defaultValue: "BEKLEMEDE"),
                    OlusturulmaTarihi = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    Aciklama = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MasaID = table.Column<int>(type: "int", nullable: false),
                    RezervasyonTipi = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    UyeID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Rezervas__CD4DF9786785B85B", x => x.RezervasyonID);
                    table.ForeignKey(
                        name: "FK_Rezervasyon_Masa",
                        column: x => x.MasaID,
                        principalTable: "Masa",
                        principalColumn: "MasaID");
                    table.ForeignKey(
                        name: "FK_Rezervasyon_Uyeler",
                        column: x => x.UyeID,
                        principalTable: "Uyeler",
                        principalColumn: "UyeID");
                });

            migrationBuilder.CreateTable(
                name: "Urun_Recetesi",
                columns: table => new
                {
                    ReceteID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UrunID = table.Column<int>(type: "int", nullable: false),
                    MalzemeID = table.Column<int>(type: "int", nullable: false),
                    KullanimMiktari = table.Column<decimal>(type: "decimal(10,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Urun_Rec__02D0477B61F5DE92", x => x.ReceteID);
                    table.ForeignKey(
                        name: "FK_UrunRecetesi_Malzemeler",
                        column: x => x.MalzemeID,
                        principalTable: "Malzemeler",
                        principalColumn: "MalzemeID");
                    table.ForeignKey(
                        name: "FK_UrunRecetesi_Urunler",
                        column: x => x.UrunID,
                        principalTable: "Urunler",
                        principalColumn: "UrunID");
                });

            migrationBuilder.CreateTable(
                name: "Kasa",
                columns: table => new
                {
                    KasaID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AcilisBakiyesi = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    KapanisBakiyesi = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    AcilisTarihi = table.Column<DateTime>(type: "datetime", nullable: false),
                    KapanisTarihi = table.Column<DateTime>(type: "datetime", nullable: true),
                    KasaDurumu = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PersonelID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Kasa__1595E41150A253EE", x => x.KasaID);
                    table.ForeignKey(
                        name: "FK_Kasa_Personel",
                        column: x => x.PersonelID,
                        principalTable: "Personel",
                        principalColumn: "PersonelID");
                });

            migrationBuilder.CreateTable(
                name: "PersonelIzin",
                columns: table => new
                {
                    IzinID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IzinBaslangic = table.Column<DateOnly>(type: "date", nullable: false),
                    IzinBitis = table.Column<DateOnly>(type: "date", nullable: false),
                    IzinDurumu = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, defaultValue: "BEKLEMEDE"),
                    IzinAciklamasi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PersonelID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Personel__4700791E92F85897", x => x.IzinID);
                    table.ForeignKey(
                        name: "FK_PersonelIzin_Personel",
                        column: x => x.PersonelID,
                        principalTable: "Personel",
                        principalColumn: "PersonelID");
                });

            migrationBuilder.CreateTable(
                name: "Siparisler",
                columns: table => new
                {
                    SiparisID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SiparisDurumu = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SiparisTipi = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ToplamTutar = table.Column<decimal>(type: "decimal(10,2)", nullable: true, defaultValue: 0m),
                    SiparisTarihi = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    UyeID = table.Column<int>(type: "int", nullable: true),
                    MasaID = table.Column<int>(type: "int", nullable: true),
                    PersonelID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Siparisl__C3F03BDD36366D4C", x => x.SiparisID);
                    table.ForeignKey(
                        name: "FK_Siparisler_Masa",
                        column: x => x.MasaID,
                        principalTable: "Masa",
                        principalColumn: "MasaID");
                    table.ForeignKey(
                        name: "FK_Siparisler_Personel",
                        column: x => x.PersonelID,
                        principalTable: "Personel",
                        principalColumn: "PersonelID");
                    table.ForeignKey(
                        name: "FK_Siparisler_Uyeler",
                        column: x => x.UyeID,
                        principalTable: "Uyeler",
                        principalColumn: "UyeID");
                });

            migrationBuilder.CreateTable(
                name: "StokHareket",
                columns: table => new
                {
                    StokHareketID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StokIslemTipi = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StokMiktari = table.Column<int>(type: "int", nullable: false),
                    IsleminTarihSaati = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    IsleminAciklamasi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UrunID = table.Column<int>(type: "int", nullable: true),
                    PersonelID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__StokHare__8F9B28C09F5FBF46", x => x.StokHareketID);
                    table.ForeignKey(
                        name: "FK_StokHareket_Personel",
                        column: x => x.PersonelID,
                        principalTable: "Personel",
                        principalColumn: "PersonelID");
                    table.ForeignKey(
                        name: "FK_StokHareket_Urunler",
                        column: x => x.UrunID,
                        principalTable: "Urunler",
                        principalColumn: "UrunID");
                });

            migrationBuilder.CreateTable(
                name: "Odeme",
                columns: table => new
                {
                    OdemeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OdemeTipi = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OdemeTutari = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    OdemeTarihi = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    PersonelID = table.Column<int>(type: "int", nullable: true),
                    SiparisID = table.Column<int>(type: "int", nullable: true),
                    KasaID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Odeme__B11B66AD0AE797C9", x => x.OdemeID);
                    table.ForeignKey(
                        name: "FK_Odeme_Kasa",
                        column: x => x.KasaID,
                        principalTable: "Kasa",
                        principalColumn: "KasaID");
                    table.ForeignKey(
                        name: "FK_Odeme_Personel",
                        column: x => x.PersonelID,
                        principalTable: "Personel",
                        principalColumn: "PersonelID");
                    table.ForeignKey(
                        name: "FK_Odeme_Siparisler",
                        column: x => x.SiparisID,
                        principalTable: "Siparisler",
                        principalColumn: "SiparisID");
                });

            migrationBuilder.CreateTable(
                name: "SiparisDetay",
                columns: table => new
                {
                    SiparisDetayID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Adet = table.Column<int>(type: "int", nullable: false),
                    BirimFiyat = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    DetayNot = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SiparisID = table.Column<int>(type: "int", nullable: false),
                    UrunID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__SiparisD__DA4BD83290A9C705", x => x.SiparisDetayID);
                    table.ForeignKey(
                        name: "FK_SiparisDetay_Siparisler",
                        column: x => x.SiparisID,
                        principalTable: "Siparisler",
                        principalColumn: "SiparisID");
                    table.ForeignKey(
                        name: "FK_SiparisDetay_Urunler",
                        column: x => x.UrunID,
                        principalTable: "Urunler",
                        principalColumn: "UrunID");
                });

            migrationBuilder.CreateTable(
                name: "Iade",
                columns: table => new
                {
                    IadeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IadeTarihi = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    IadeSebebi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IadeDurumu = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, defaultValue: "BEKLEMEDE"),
                    IadeTutari = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    SiparisDetayID = table.Column<int>(type: "int", nullable: true),
                    UrunID = table.Column<int>(type: "int", nullable: true),
                    PersonelID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Iade__D047997FB4226E2E", x => x.IadeID);
                    table.ForeignKey(
                        name: "FK_Iade_Personel",
                        column: x => x.PersonelID,
                        principalTable: "Personel",
                        principalColumn: "PersonelID");
                    table.ForeignKey(
                        name: "FK_Iade_SiparisDetay",
                        column: x => x.SiparisDetayID,
                        principalTable: "SiparisDetay",
                        principalColumn: "SiparisDetayID");
                    table.ForeignKey(
                        name: "FK_Iade_Urunler",
                        column: x => x.UrunID,
                        principalTable: "Urunler",
                        principalColumn: "UrunID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Adres_UyeID",
                table: "Adres",
                column: "UyeID");

            migrationBuilder.CreateIndex(
                name: "IX_Iade_PersonelID",
                table: "Iade",
                column: "PersonelID");

            migrationBuilder.CreateIndex(
                name: "IX_Iade_SiparisDetayID",
                table: "Iade",
                column: "SiparisDetayID");

            migrationBuilder.CreateIndex(
                name: "IX_Iade_UrunID",
                table: "Iade",
                column: "UrunID");

            migrationBuilder.CreateIndex(
                name: "IX_Kasa_PersonelID",
                table: "Kasa",
                column: "PersonelID");

            migrationBuilder.CreateIndex(
                name: "IX_MalzemeTalepleri_MalzemeId",
                table: "MalzemeTalepleri",
                column: "MalzemeId");

            migrationBuilder.CreateIndex(
                name: "IX_Odeme_KasaID",
                table: "Odeme",
                column: "KasaID");

            migrationBuilder.CreateIndex(
                name: "IX_Odeme_PersonelID",
                table: "Odeme",
                column: "PersonelID");

            migrationBuilder.CreateIndex(
                name: "IX_Odeme_SiparisID",
                table: "Odeme",
                column: "SiparisID");

            migrationBuilder.CreateIndex(
                name: "IX_Personel_RolID",
                table: "Personel",
                column: "RolID");

            migrationBuilder.CreateIndex(
                name: "IX_PersonelIzin_PersonelID",
                table: "PersonelIzin",
                column: "PersonelID");

            migrationBuilder.CreateIndex(
                name: "IX_Rezervasyon_MasaID",
                table: "Rezervasyon",
                column: "MasaID");

            migrationBuilder.CreateIndex(
                name: "IX_Rezervasyon_UyeID",
                table: "Rezervasyon",
                column: "UyeID");

            migrationBuilder.CreateIndex(
                name: "IX_SiparisDetay_SiparisID",
                table: "SiparisDetay",
                column: "SiparisID");

            migrationBuilder.CreateIndex(
                name: "IX_SiparisDetay_UrunID",
                table: "SiparisDetay",
                column: "UrunID");

            migrationBuilder.CreateIndex(
                name: "IX_Siparisler_MasaID",
                table: "Siparisler",
                column: "MasaID");

            migrationBuilder.CreateIndex(
                name: "IX_Siparisler_PersonelID",
                table: "Siparisler",
                column: "PersonelID");

            migrationBuilder.CreateIndex(
                name: "IX_Siparisler_UyeID",
                table: "Siparisler",
                column: "UyeID");

            migrationBuilder.CreateIndex(
                name: "IX_StokHareket_PersonelID",
                table: "StokHareket",
                column: "PersonelID");

            migrationBuilder.CreateIndex(
                name: "IX_StokHareket_UrunID",
                table: "StokHareket",
                column: "UrunID");

            migrationBuilder.CreateIndex(
                name: "IX_Urun_Recetesi_MalzemeID",
                table: "Urun_Recetesi",
                column: "MalzemeID");

            migrationBuilder.CreateIndex(
                name: "IX_Urun_Recetesi_UrunID",
                table: "Urun_Recetesi",
                column: "UrunID");

            migrationBuilder.CreateIndex(
                name: "IX_Urunler_KategoriID",
                table: "Urunler",
                column: "KategoriID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Adres");

            migrationBuilder.DropTable(
                name: "Iade");

            migrationBuilder.DropTable(
                name: "MalzemeTalepleri");

            migrationBuilder.DropTable(
                name: "Odeme");

            migrationBuilder.DropTable(
                name: "PersonelIzin");

            migrationBuilder.DropTable(
                name: "Rezervasyon");

            migrationBuilder.DropTable(
                name: "StokHareket");

            migrationBuilder.DropTable(
                name: "Urun_Recetesi");

            migrationBuilder.DropTable(
                name: "SiparisDetay");

            migrationBuilder.DropTable(
                name: "Kasa");

            migrationBuilder.DropTable(
                name: "Malzemeler");

            migrationBuilder.DropTable(
                name: "Siparisler");

            migrationBuilder.DropTable(
                name: "Urunler");

            migrationBuilder.DropTable(
                name: "Masa");

            migrationBuilder.DropTable(
                name: "Personel");

            migrationBuilder.DropTable(
                name: "Uyeler");

            migrationBuilder.DropTable(
                name: "Kategori");

            migrationBuilder.DropTable(
                name: "Roller");
        }
    }
}
