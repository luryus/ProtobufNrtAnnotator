using Microsoft.Build.Framework;

namespace ProtobufNrtAnnotator;

public class NrtAnnotateProtobufFiles : Microsoft.Build.Utilities.Task
{
    [Required]
    public ITaskItem[] Files { get; set; } = [];

    public override bool Execute()
    {
        foreach (var filePath in Files.Select(x => x.ItemSpec))
        {
            if (!File.Exists(filePath))
            {
                continue;
            }
            var code = File.ReadAllText(filePath);

            var newCode = Runner.ProcessContent(code);
            if (newCode == null)
            {
                Log.LogMessage($"Skipping {filePath} (not a processable protobuf file)");
                continue;
            }

            Log.LogMessage($"Processing {filePath}...");
            File.WriteAllText(filePath, newCode);
        }

        return true;
    }
}