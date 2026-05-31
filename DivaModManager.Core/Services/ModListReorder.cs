using System.Collections.ObjectModel;
using DivaModManager.Core.Models;

namespace DivaModManager.Core.Services;

public static class ModListReorder
{
    public static bool MoveItem(ObservableCollection<Mod> list, Mod dragged, Mod target)
    {
        var oldIndex = list.IndexOf(dragged);
        var newIndex = list.IndexOf(target);
        if (oldIndex < 0 || newIndex < 0 || oldIndex == newIndex)
        {
            return false;
        }

        list.Move(oldIndex, newIndex);
        return true;
    }
}
