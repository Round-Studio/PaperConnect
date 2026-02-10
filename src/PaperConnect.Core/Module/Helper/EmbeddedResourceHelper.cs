using System.Reflection;

namespace PaperConnect.Core.Module.Helper;

public class EmbeddedResourceHelper
{
    // 获取当前程序集中所有嵌入式资源的名称
    public static List<string> GetAllResourceNames()
    {
        var assembly = Assembly.GetExecutingAssembly();
        return assembly.GetManifestResourceNames().ToList();
    }

    // 根据部分名称查找资源
    public static string FindResourceName(string partialName)
    {
        var allResources = GetAllResourceNames();
        return allResources.FirstOrDefault(r => r.Contains(partialName));
    }

    // 按文件夹分组显示资源
    public static void DisplayAllResources()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resources = assembly.GetManifestResourceNames();
        
        Console.WriteLine("程序集中的嵌入式资源:");
        foreach (var resource in resources)
        {
            Console.WriteLine($"  {resource}");
        }
    }
    
    public static string ReadEmbeddedResource(Assembly assembly, string resourceName)
    {
        using (var stream = assembly.GetManifestResourceStream(resourceName))
        {
            if (stream == null)
                throw new ArgumentException($"资源 '{resourceName}' 未找到", nameof(resourceName));
            
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }
}