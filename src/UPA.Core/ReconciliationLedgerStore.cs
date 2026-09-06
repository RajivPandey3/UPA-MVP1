using System.Text.Json;

namespace UPA.Core;

public sealed class ReconciliationLedgerStore
{
    private readonly JsonSerializerOptions options = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public void Save(string path, ReconciliationEventLedger ledger)
    {
        ArgumentNullException.ThrowIfNull(ledger);
        var fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        var temporary = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(ledger.Events, options));
        File.Move(temporary, fullPath, true);
    }

    public ReconciliationEventLedger Load(string path)
    {
        var fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath)) throw new FileNotFoundException("Ledger was not found.", fullPath);
        var events = JsonSerializer.Deserialize<ReconciliationEvent[]>(File.ReadAllText(fullPath), options)
            ?? throw new InvalidDataException("Ledger is empty.");
        var ledger = new ReconciliationEventLedger();
        ledger.Append(events);
        return ledger;
    }
}
