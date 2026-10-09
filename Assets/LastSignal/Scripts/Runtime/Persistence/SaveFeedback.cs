namespace LastSignal.Persistence
{
    /// <summary>Player-facing text only; SaveResult remains the transaction authority.</summary>
    public static class SaveFeedback
    {
        public static string Describe(SaveResult result, bool load)
        {
            if (result.Success)
                return load ? "Kontrol noktası yüklendi." : "Kontrol noktası kaydedildi.";

            string reason;
            switch (result.Error)
            {
                case SaveError.MissingFile: reason = "Kayıt dosyası bulunamadı."; break;
                case SaveError.InvalidJson:
                case SaveError.ChecksumMismatch: reason = "Kayıt dosyası bozuk veya eksik."; break;
                case SaveError.InvalidData:
                case SaveError.DuplicateIdentity: reason = "Oyun durumu veya kayıt verisi geçersiz."; break;
                case SaveError.UnsupportedSchema: reason = "Kayıt sürümü desteklenmiyor."; break;
                case SaveError.IncompatibleContent:
                case SaveError.UnknownDefinition: reason = "Kayıt, mevcut oyun içeriğiyle uyumlu değil."; break;
                case SaveError.WrongWorld: reason = "Kayıt başka bir dünyaya ait."; break;
                case SaveError.TooLarge: reason = "Kayıt boyutu sınırı aşıldı."; break;
                case SaveError.IoFailure: reason = "Dosyaya erişilemedi. Depolama alanını ve izinleri kontrol et."; break;
                case SaveError.Busy: reason = "Şu anda yapılamıyor. Menüde yükle; oyunu duraklatarak kaydet."; break;
                case SaveError.StaleSession:
                case SaveError.StaleGeneration: reason = "Oturum değiştiği için işlem tamamlanamadı."; break;
                default: reason = "Beklenmeyen bir hata oluştu."; break;
            }
            return (load ? "Kayıt yüklenemedi. " : "Kayıt oluşturulamadı. ") + reason;
        }
    }
}
