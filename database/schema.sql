-- =============================================================================
-- Kademeli Bilişim ve Siber Güvenlik Eğitim Platformu
-- PostgreSQL 18 Veritabanı Şeması (kademeli_egitim)
-- =============================================================================
-- Kurulum:
--   1) psql -U postgres -c "CREATE DATABASE kademeli_egitim ENCODING 'UTF8';"
--   2) psql -U postgres -d kademeli_egitim -f database/schema.sql
-- =============================================================================

SET client_encoding = 'UTF8';

-- -----------------------------------------------------------------------------
-- 1. KULLANICILAR TABLOSU (BCrypt.Net-Next ile tuzlu parola özetleme)
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS kullanicilar (
    id              SERIAL PRIMARY KEY,
    kullanici_adi   VARCHAR(50)  NOT NULL UNIQUE,
    ad_soyad        VARCHAR(100) NOT NULL DEFAULT '',
    sifre_hash      VARCHAR(72)  NOT NULL, -- BCrypt.Net-Next ($2a$ / $2b$) tuzlu özet
    rol             VARCHAR(20)  NOT NULL DEFAULT 'Ogrenci'
                    CHECK (rol IN ('Ogrenci', 'Egitmen', 'Yonetici')),
    aktif_mi        BOOLEAN      NOT NULL DEFAULT TRUE,
    olusturma_tarihi TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    son_giris_tarihi TIMESTAMPTZ NULL
);

CREATE INDEX IF NOT EXISTS ix_kullanicilar_kullanici_adi
    ON kullanicilar (kullanici_adi);

-- -----------------------------------------------------------------------------
-- 2. SORULAR TABLOSU (ClosedXML .xlsx ve .csv içe/dışa aktarım uyumlu)
-- Disiplin: 0 = GorselProgramlama, 1 = VeriTabanlari, 2 = SiberGuvenlik
-- Zorluk  : 0 = Kolay, 1 = Orta, 2 = Zor
-- Etiket  : NULL (Normal Sınav), 'AraSinav', 'KolayOrtaAraSinav', 'OrtaZorAraSinav'
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS sorular (
    id                      SERIAL PRIMARY KEY,
    disiplin                SMALLINT     NOT NULL CHECK (disiplin BETWEEN 0 AND 2),
    zorluk                  SMALLINT     NOT NULL CHECK (zorluk BETWEEN 0 AND 2),
    etiket                  VARCHAR(50)  NULL,
    baslik                  TEXT         NOT NULL UNIQUE,
    kod_blogu               TEXT         NULL,
    secenek_a               TEXT         NOT NULL,
    secenek_b               TEXT         NOT NULL,
    secenek_c               TEXT         NOT NULL,
    secenek_d               TEXT         NOT NULL,
    dogru_secenek_index     SMALLINT     NOT NULL CHECK (dogru_secenek_index BETWEEN 0 AND 3),
    aciklama_a              TEXT         NULL,
    aciklama_b              TEXT         NULL,
    aciklama_c              TEXT         NULL,
    aciklama_d              TEXT         NULL,
    olusturma_tarihi        TIMESTAMPTZ  NOT NULL DEFAULT CURRENT_TIMESTAMP,
    guncelleme_tarihi       TIMESTAMPTZ  NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS ix_sorular_disiplin_zorluk
    ON sorular (disiplin, zorluk);

CREATE INDEX IF NOT EXISTS ix_sorular_etiket
    ON sorular (etiket);

-- -----------------------------------------------------------------------------
-- 3. KULLANICI İLERLEME VE SEVİYE GEÇİŞ TABLOSU
-- Katı engel yerine Kolay -> Orta ve Orta -> Zor geçişlerinde
-- "Seviye Geçiş Ara Sınavı" durumunu ve başarı metriklerini saklar.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS kullanici_ilerleme (
    id                          SERIAL PRIMARY KEY,
    kullanici_id                INTEGER      NULL UNIQUE REFERENCES kullanicilar(id) ON DELETE CASCADE,
    gorsel_basari               INTEGER      NOT NULL DEFAULT 0 CHECK (gorsel_basari BETWEEN 0 AND 100),
    veritabani_basari           INTEGER      NOT NULL DEFAULT 0 CHECK (veritabani_basari BETWEEN 0 AND 100),
    siber_basari                INTEGER      NOT NULL DEFAULT 0 CHECK (siber_basari BETWEEN 0 AND 100),
    ipucu_jetonu                INTEGER      NOT NULL DEFAULT 3 CHECK (ipucu_jetonu >= 0),
    hata_avcisi_kazanildi       BOOLEAN      NOT NULL DEFAULT FALSE,
    gorsel_ara_sinav_gecti      BOOLEAN      NOT NULL DEFAULT FALSE,
    veritabani_ara_sinav_gecti  BOOLEAN      NOT NULL DEFAULT FALSE,
    siber_ara_sinav_gecti       BOOLEAN      NOT NULL DEFAULT FALSE,
    kolay_orta_ara_sinav_gecti  BOOLEAN      NOT NULL DEFAULT FALSE,
    orta_zor_ara_sinav_gecti    BOOLEAN      NOT NULL DEFAULT FALSE,
    orta_mod_gecti              BOOLEAN      NOT NULL DEFAULT FALSE,
    aktif_kademe                SMALLINT     NOT NULL DEFAULT 0 CHECK (aktif_kademe BETWEEN 0 AND 2),
    son_guncelleme              TIMESTAMPTZ  NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Anahtar-değer tabanlı mevcut IlerlemeServisi uyumluluk tablosu
CREATE TABLE IF NOT EXISTS ilerleme (
    anahtar VARCHAR(100) PRIMARY KEY,
    deger   INTEGER      NOT NULL
);

-- -----------------------------------------------------------------------------
-- 4. SEVİYE GEÇİŞ ARA SINAVI VE SINAV GEÇMİŞİ TABLOSU
-- Kolay -> Orta ve Orta -> Zor seviye geçiş ara sınav denemelerini kaydeder.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS seviye_gecis_sinavlari (
    id                  SERIAL PRIMARY KEY,
    kullanici_id        INTEGER      NULL REFERENCES kullanicilar(id) ON DELETE SET NULL,
    gecis_turu          VARCHAR(30)  NOT NULL
                        CHECK (gecis_turu IN ('KolaydanOrtaya', 'OrtadanZora', 'ModulAraSinav', 'NormalSinav')),
    disiplin            SMALLINT     NULL CHECK (disiplin IS NULL OR (disiplin BETWEEN 0 AND 2)),
    hedef_zorluk        SMALLINT     NOT NULL CHECK (hedef_zorluk BETWEEN 0 AND 2),
    toplam_soru         INTEGER      NOT NULL CHECK (toplam_soru > 0),
    dogru_sayisi        INTEGER      NOT NULL CHECK (dogru_sayisi >= 0 AND dogru_sayisi <= toplam_soru),
    basari_yuzdesi      INTEGER      NOT NULL CHECK (basari_yuzdesi BETWEEN 0 AND 100),
    baraj_yuzdesi       INTEGER      NOT NULL DEFAULT 70 CHECK (baraj_yuzdesi BETWEEN 0 AND 100),
    basarili_mi         BOOLEAN      NOT NULL,
    gecen_sure_saniye   INTEGER      NOT NULL DEFAULT 0 CHECK (gecen_sure_saniye >= 0),
    sinav_tarihi        TIMESTAMPTZ  NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS ix_seviye_gecis_sinavlari_kullanici_tarih
    ON seviye_gecis_sinavlari (kullanici_id, sinav_tarihi DESC);

-- -----------------------------------------------------------------------------
-- 5. YANLIŞ CEVAPLAR (HATA KARNESİ) TABLOSU
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS yanlis_cevaplar (
    id              SERIAL PRIMARY KEY,
    kullanici_id    INTEGER      NULL REFERENCES kullanicilar(id) ON DELETE CASCADE,
    soru_id         INTEGER      NULL REFERENCES sorular(id) ON DELETE SET NULL,
    tarih           TIMESTAMPTZ  NOT NULL DEFAULT CURRENT_TIMESTAMP,
    disiplin        SMALLINT     NOT NULL CHECK (disiplin BETWEEN 0 AND 2),
    zorluk          SMALLINT     NOT NULL CHECK (zorluk BETWEEN 0 AND 2),
    baslik          TEXT         NOT NULL,
    secilen_cevap   TEXT         NOT NULL,
    dogru_cevap     TEXT         NOT NULL,
    aciklama        TEXT         NOT NULL DEFAULT ''
);

CREATE INDEX IF NOT EXISTS ix_yanlis_cevaplar_tarih
    ON yanlis_cevaplar (tarih DESC, id DESC);

-- -----------------------------------------------------------------------------
-- 6. UYGULAMA META VERİLERİ TABLOSU
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS meta (
    anahtar VARCHAR(100) PRIMARY KEY,
    deger   TEXT         NOT NULL
);

INSERT INTO meta (anahtar, deger)
VALUES ('semaSurumu', '3')
ON CONFLICT (anahtar) DO NOTHING;
