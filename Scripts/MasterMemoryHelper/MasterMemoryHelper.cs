using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using MessagePack;
using MessagePack.Resolvers;

namespace MasterMemoryHelper
{
    public class MasterMemoryGenerator : MonoBehaviour
    {
        private const string BinaryFolderPath = "Packages/space.mkim.mastermemoryhelper/Binary/";

        private static readonly IReadOnlyDictionary<RuntimePlatform, string> m_osNameByPlatform = new Dictionary<RuntimePlatform, string>
        {
            {RuntimePlatform.WindowsEditor, "win" },
            {RuntimePlatform.OSXEditor, "osx" },
            {RuntimePlatform.LinuxEditor, "linux" },
        };

        [MenuItem("MasterMemoryHelper/GenerateScripts")]
        private static void GenerateScripts()
        {
            var guids = AssetDatabase.FindAssets($"t: {typeof(Configuration)}");

            var assetPath = AssetDatabase.GUIDToAssetPath(guids.First());
            var asset = AssetDatabase.LoadAssetAtPath<Configuration>(assetPath);

            _ = GenerateAsync(asset);
        }

        [MenuItem("MasterMemoryHelper/GenerateBinaryFromCsv")]
        private static void GenerateBinaryFromCsv()
        {
            var guids = AssetDatabase.FindAssets($"t: {typeof(Configuration)}");

            var assetPath = AssetDatabase.GUIDToAssetPath(guids.First());
            var asset = AssetDatabase.LoadAssetAtPath<Configuration>(assetPath);

            try
            {
                RegisterResolvers(asset);
                GenerateBinaryFromCsv(asset);

                EditorUtility.DisplayDialog("成功", "成功しました。", "確認");
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);

                EditorUtility.DisplayDialog("失敗", "失敗しました。エラーログを確認してください。", "確認");
            }
            finally
            {
                AssetDatabase.Refresh();
            }
        }

        private static async Task GenerateAsync(Configuration configuration)
        {
            try
            {
                if (string.IsNullOrEmpty(configuration.ScriptInputPath) || string.IsNullOrEmpty(configuration.ScriptOutputPath) || string.IsNullOrEmpty(configuration.Namespace))
                {
                    throw new Exception("引数が足りてないです。設定を確認してください。");
                }

                var scriptInputFullPath = Path.Combine(Application.dataPath, configuration.ScriptInputPath);
                var scriptOutputFullPath = Path.Combine(Application.dataPath, configuration.ScriptOutputPath);

                Directory.CreateDirectory(Path.GetDirectoryName(scriptInputFullPath));
                Directory.CreateDirectory(Path.GetDirectoryName(scriptOutputFullPath));

                await GenerateMasterMemory(scriptInputFullPath, scriptOutputFullPath, configuration.Namespace, configuration.PrefixClassName);
                await GenerateMessagePack(scriptInputFullPath, scriptOutputFullPath, configuration.Namespace);

                EditorUtility.DisplayDialog("成功", "成功しました。", "確認");
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);

                EditorUtility.DisplayDialog("失敗", "失敗しました。エラーログを確認してください。", "確認");

                throw;
            }
            finally
            {
                AssetDatabase.Refresh();
            }
        }

        private static void RegisterResolvers(Configuration configuration)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var assembliesByFullName = assemblies.SelectMany(x => x.GetExportedTypes()).ToLookup(x => x.FullName);

            var memoryResolverType = assembliesByFullName[$"{configuration.Namespace}.{configuration.PrefixClassName}MasterMemoryResolver"].First();
            var generatedResolverType = assembliesByFullName[$"{configuration.Namespace}.Resolvers.GeneratedResolver"].First();

            var resolver = CompositeResolver.Create(
                memoryResolverType.GetField("Instance").GetValue(null) as MessagePack.IFormatterResolver,
                generatedResolverType.GetField("Instance").GetValue(null) as MessagePack.IFormatterResolver,
                StandardResolver.Instance
            );

            var options = MessagePackSerializerOptions.Standard.WithResolver(resolver);
            MessagePackSerializer.DefaultOptions = options;
        }

        private static void GenerateBinaryFromCsv(Configuration configuration)
        {
            if (string.IsNullOrEmpty(configuration.CsvInputPath) || string.IsNullOrEmpty(configuration.BinaryOutputPath))
            {
                throw new Exception("引数が足りてないです。設定を確認してください。");
            }

            var csvInputFullPath = Path.Combine(Application.dataPath, configuration.CsvInputPath);
            var binaryOutputFullPath = Path.Combine(Application.dataPath, configuration.BinaryOutputPath);

            Directory.CreateDirectory(Path.GetDirectoryName(binaryOutputFullPath));

            foreach (var csvInputFilePath in Directory.GetFiles(csvInputFullPath).Where(x => Path.GetExtension(x) != ".meta"))
            {
                CsvToDatabaseBinary.Convert(csvInputFilePath, binaryOutputFullPath, configuration.DatabaseBuilderType, configuration.MemoryDatabaseType);
            }
        }

        private static async Task GenerateMasterMemory(string inputPath, string outputPath, string usingNamespace, string prefixClassName)
        {
            var assetPath = Path.Combine(BinaryFolderPath, $"MasterMemory.Generator/{GetOSNameFromCurrentRuntimePlatform()}-x64/MasterMemory.Generator");
            var binaryFullPath = Path.GetFullPath(assetPath);

            var parsedPrefixClassName = string.IsNullOrEmpty(prefixClassName) ? string.Empty : $"-p {prefixClassName}";
            var arg = $"-i {inputPath} -o {outputPath} -n {usingNamespace} {parsedPrefixClassName}";

            await RunProcessAsync(binaryFullPath, arg, string.Empty);
        }

        private static async Task GenerateMessagePack(string inputPath, string outputPath, string usingNamespace)
        {
            var assetPath = Path.Combine(BinaryFolderPath, $"mpc/{GetOSNameFromCurrentRuntimePlatform()}/mpc");
            var binaryFullPath = Path.GetFullPath(assetPath);

            var parsedNamespace = string.IsNullOrEmpty(usingNamespace) ? string.Empty : $"-n {usingNamespace}";
            var arg = $"-i {inputPath} -o {outputPath} {parsedNamespace}";

            await RunProcessAsync(binaryFullPath, arg, string.Empty);
        }

        private static string GetOSNameFromCurrentRuntimePlatform()
        {
            if (!m_osNameByPlatform.TryGetValue(Application.platform, out var osName))
            {
                throw new Exception("サポートしないOSです。");
            }

            return osName;
        }

        private static async Task RunProcessAsync(string fileName, string argument, string workingDirectory)
        {
            var startInfo = new ProcessStartInfo(fileName, argument)
            {
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            using (var process = new Process() { StartInfo = startInfo, EnableRaisingEvents = true })
            {
                var taskSource = new TaskCompletionSource<bool>();

                process.OutputDataReceived += (_, e) =>
                {
                    if (string.IsNullOrEmpty(e.Data)) return;

                    UnityEngine.Debug.Log(e.Data);
                };

                process.ErrorDataReceived += (_, e) =>
                {
                    if (string.IsNullOrEmpty(e.Data)) return;

                    UnityEngine.Debug.LogError(e.Data);
                };

                process.Exited += (_, __) => taskSource.SetResult(true);

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await taskSource.Task;

                process.CancelOutputRead();
                process.CancelErrorRead();
            }
        }

        private static void Hoge()
        {

        }

    }
}
