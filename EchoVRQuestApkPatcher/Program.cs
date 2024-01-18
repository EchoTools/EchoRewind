using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using BsDiff;
using Newtonsoft.Json.Linq;


static class Program
{
    static class Hashes
    {
        public const string APK = "c14c0f68adb62a4c5deaef46d046f872"; // Hash of 
    }

    static string CalculateMD5(string filename)
    {
        using (var md5 = MD5.Create())
        {
            using (var stream = File.OpenRead(filename))
            {
                var hash = md5.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }
    }

    static void ExitLog(string errorString, bool error = true)
    {
        if (error)
        {
            Console.Error.WriteLine(errorString);
        }
        else
        {
            Console.WriteLine(errorString);
        }
        Console.WriteLine("Press any key to exit");
        Console.ReadKey();
        Environment.Exit(0);
    }


    static bool CheckJson(JObject jsonObject)
    {
        // Make sure the services exist
        if (!jsonObject.ContainsKey("configservice_host"))
            return false;
        if (!jsonObject.ContainsKey("loginservice_host"))
            return false;
        if (!jsonObject.ContainsKey("matchingservice_host"))
            return false;
        if (!jsonObject.ContainsKey("publisher_lock"))
            return false;

        // Make sure they are all strings
        if (jsonObject.GetValue("configservice_host")!.Type != JTokenType.String)
            return false;
        if (jsonObject.GetValue("loginservice_host")!.Type != JTokenType.String)
            return false;
        if (jsonObject.GetValue("matchingservice_host")!.Type != JTokenType.String)
            return false;
        if (jsonObject.GetValue("publisher_lock")!.Type != JTokenType.String)
            return false;

        // Make sure the hosts are valid URLs
        if (!Uri.IsWellFormedUriString(jsonObject.Value<string>("configservice_host"), UriKind.Absolute))
            return false;
        if (!Uri.IsWellFormedUriString(jsonObject.Value<string>("loginservice_host"), UriKind.Absolute))
            return false;
        if (!Uri.IsWellFormedUriString(jsonObject.Value<string>("matchingservice_host"), UriKind.Absolute))
            return false;

        return true;
    }

    static void CheckPrerequisites(string originalApkPath, string configPath)
    {
        if (!File.Exists(originalApkPath))
            ExitLog("Invalid EchoVR APK: Please drag and drop EchoVR APK onto exe");

        if (CalculateMD5(originalApkPath) != Hashes.APK)
            ExitLog("Invalid EchoVR APK (Hash mismatch) : please download the correct APK via\nOculusDB: https://oculusdb.rui2015.me/id/2215004568539258\nVersion: 4987566");

        if (!File.Exists(configPath))
            ExitLog("Invalid Config: Config not found, please confirm config is in the same directory as the executable");

        string ConfigString;
        try
        {
            ConfigString = File.ReadAllText(configPath);
        } catch (Exception)
        {
            ExitLog("Invalid Config: Config stream unreachable, please confirm no other programs are modifying config.json");
            return; // Just to make the compiler happy
        }

        JObject ConfigJson;
        try
        {
            ConfigJson = JObject.Parse(ConfigString);
        }
        catch (Exception)
        {
            ExitLog("Invalid Config: Json could not be parsed, please confirm config formatting is correct");
            return;
        }

        if (!CheckJson(ConfigJson))
            ExitLog("Invalid Config: Service endpoints incorrect, please confirm all endpoints are correct");

        if (Assembly.GetExecutingAssembly().GetManifestResourceInfo("EchoVRQuestApkPatcher.libpnsovr_patch.bin") == null)
            ExitLog("Error getting ovr patch");

        if (Assembly.GetExecutingAssembly().GetManifestResourceInfo("EchoVRQuestApkPatcher.libr15_patch.bin") == null)
            ExitLog("Error getting r15 patch");
    }

    static void Main(string[] args)
    {
        Console.WriteLine("Parsing arguments...");
        if (args.Length == 0)
            ExitLog("Invalid EchoVR APK: Please drag and drop EchoVR APK onto exe");

        Console.WriteLine("Generating paths...");
        var originalApkPath = args[0];
        var baseDir = Path.GetDirectoryName(args[0]);
        var newApkPath = Path.Join(baseDir, "r15_goldmaster_store_patched.apk");
        var configPath = Path.Join(baseDir, "config.json");

        Console.WriteLine("Checking prerequisites...");
        CheckPrerequisites(originalApkPath, configPath);

        Console.WriteLine("Creating extraction directory...");
        var extractedApkDir = Path.Join(Path.GetTempPath(), "EchoQuestUnzip");
        if (Directory.Exists(extractedApkDir))
            Directory.Delete(extractedApkDir, true);
        Directory.CreateDirectory(extractedApkDir);

        Console.WriteLine("Extracting files...");
        ZipFile.ExtractToDirectory(args[0], extractedApkDir);
        var extractedLocalPath = Path.Join(extractedApkDir, "assets", "_local");
        var extractedPnsRadOvrPath = Path.Join(extractedApkDir, @"lib", "arm64-v8a", "libpnsovr.so");
        var extractedr15Path = Path.Join(extractedApkDir, @"lib", "arm64-v8a", "libr15.so");

        Console.WriteLine("Copying config.json...");
        Directory.CreateDirectory(extractedLocalPath); // No need to check for existence, as the hash will capture that
        File.Copy(configPath, Path.Join(extractedLocalPath, "config.json"));

        Console.WriteLine("Patching pnsradovr.so...");
        using var oldPnsOvrFile = File.OpenRead(extractedPnsRadOvrPath);
        using var newPnsOvrFile = File.Create(extractedPnsRadOvrPath + "_patched");
        BinaryPatch.Apply(oldPnsOvrFile, () => Assembly.GetExecutingAssembly().GetManifestResourceStream("EchoVRQuestApkPatcher.libpnsovr_patch.bin"), newPnsOvrFile);
        oldPnsOvrFile.Close();
        newPnsOvrFile.Close();

        Console.WriteLine("Patching libr15.so...");
        using var oldr15File = File.OpenRead(extractedr15Path);
        using var newr15File = File.Create(extractedr15Path + "_patched");
        BinaryPatch.Apply(oldr15File, () => Assembly.GetExecutingAssembly().GetManifestResourceStream("EchoVRQuestApkPatcher.libr15_patch.bin"), newr15File);
        oldr15File.Close();
        newr15File.Close();

        Console.WriteLine("Swapping pnsradovr.so...");
        File.Delete(extractedPnsRadOvrPath);
        File.Move(extractedPnsRadOvrPath + "_patched", extractedPnsRadOvrPath);

        Console.WriteLine("Swapping libr15.so...");
        File.Delete(extractedr15Path);
        File.Move(extractedr15Path + "_patched", extractedr15Path);

        Console.WriteLine("Creating miscellaneous directory...");
        string miscDir = Path.Join(Path.GetTempPath(), "EchoQuest");
        if (Directory.Exists(miscDir))
            Directory.Delete(miscDir, true);
        Directory.CreateDirectory(miscDir);

        Console.WriteLine("Creating unsigned apk...");
        var unsignedApkPath = Path.Join(miscDir, "unsigned.apk");
        ZipFile.CreateFromDirectory(extractedApkDir, unsignedApkPath);

        Console.WriteLine("Signing unsigned apk...");
        var unsignedApkSteam = File.Open(unsignedApkPath, FileMode.Open);
        //sign APK (this is how you do it with this lib)
        QuestPatcher.Zip.ApkZip.Open(unsignedApkSteam).Dispose();
        unsignedApkSteam.Close();

        Console.WriteLine("Moving signed apk...");
        if (File.Exists(newApkPath))
            File.Delete(newApkPath);
        File.Move(unsignedApkPath, newApkPath);

        Console.WriteLine("Cleaning up temporary files...");
        Directory.Delete(extractedApkDir, true);
        Directory.Delete(miscDir, true);

        ExitLog("Finished creating patched apk! (r15_goldmaster_store_patched.apk)", false);
    }
}
