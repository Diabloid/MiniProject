using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace NovaGames.UI.Services
{
    public class TokenStorageService
    {
        // Шлях до прихованої папки AppData/Roaming/NovaGames/auth.dat
        private readonly string _filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NovaGames", "auth.dat");

        public void SaveTokens(string token, string refreshToken)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
            string combined = $"{token}|{refreshToken}";
            byte[] encryptedData = ProtectedData.Protect(Encoding.UTF8.GetBytes(combined), null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(_filePath, encryptedData);
        }

        public (string? Token, string? RefreshToken) GetTokens()
        {
            if (!File.Exists(_filePath)) return (null, null);
            try
            {
                byte[] encryptedData = File.ReadAllBytes(_filePath);
                byte[] decryptedData = ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.CurrentUser);
                string combined = Encoding.UTF8.GetString(decryptedData);
                var parts = combined.Split('|');
                if (parts.Length == 2) return (parts[0], parts[1]);
            }
            catch { }
            return (null, null);
        }

        public string? GetToken()
        {
            if (!File.Exists(_filePath)) return null;

            try
            {
                byte[] encryptedData = File.ReadAllBytes(_filePath);

                // Розшифровуємо
                byte[] decryptedData = ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(decryptedData);
            }
            catch
            {
                return null; // Якщо файл пошкоджено або його пробує відкрити інший юзер Windows
            }
        }

        public void ClearToken()
        {
            if (File.Exists(_filePath)) File.Delete(_filePath);
        }
    }
}