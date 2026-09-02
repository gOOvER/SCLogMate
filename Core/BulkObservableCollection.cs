using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace SCLogMate.Core;

/// <summary>
/// ObservableCollection mit <see cref="ReplaceAll"/> – tauscht den kompletten Inhalt aus und
/// feuert dabei nur EIN einziges Reset-Ereignis. Beim Befüllen der Event-Tabelle (≈60k Zeilen)
/// vermeidet das die O(n²)-Bremse, bei der eine daran hängende CollectionView bei jedem
/// Einzel-Add neu filtern/sortieren würde.
/// </summary>
public class BulkObservableCollection<T> : ObservableCollection<T>
{
    private static readonly PropertyChangedEventArgs CountArgs = new(nameof(Count));
    private static readonly PropertyChangedEventArgs IndexerArgs = new("Item[]");

    public void ReplaceAll(IEnumerable<T> items)
    {
        Items.Clear();
        foreach (var i in items) Items.Add(i);
        OnPropertyChanged(CountArgs);
        OnPropertyChanged(IndexerArgs);
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
}
