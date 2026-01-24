using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using AutoClicker.Models;

namespace AutoClicker.Helpers
{
    public class ProfileManager
    {
        private const string ProfilesFolder = "Profiles";
        private const string ProfileExtension = ".json";

        public ProfileManager()
        {
            if (!Directory.Exists(ProfilesFolder))
            {
                Directory.CreateDirectory(ProfilesFolder);
            }
        }

        public void SaveProfile(Profile profile)
        {
            if (string.IsNullOrWhiteSpace(profile.Name))
            {
                throw new ArgumentException("プロファイル名を指定してください。");
            }

            string filePath = GetProfilePath(profile.Name);
            string json = JsonSerializer.Serialize(profile, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }

        public Profile? LoadProfile(string profileName)
        {
            string filePath = GetProfilePath(profileName);
            if (!File.Exists(filePath))
            {
                return null;
            }

            string json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<Profile>(json);
        }

        public ObservableCollection<string> GetProfileNames()
        {
            var profiles = new ObservableCollection<string>();
            if (Directory.Exists(ProfilesFolder))
            {
                foreach (string file in Directory.GetFiles(ProfilesFolder, $"*{ProfileExtension}"))
                {
                    profiles.Add(Path.GetFileNameWithoutExtension(file));
                }
            }
            return profiles;
        }

        public void DeleteProfile(string profileName)
        {
            string filePath = GetProfilePath(profileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        private string GetProfilePath(string profileName)
        {
            return Path.Combine(ProfilesFolder, $"{profileName}{ProfileExtension}");
        }
    }
}
