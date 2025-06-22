using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RelinkToolkit2.ViewModels.Documents.GraphEditor;

public class QuestContext
{
    public uint Category { get; }
    public uint SubCategory { get; }
    public uint Index { get; }
    public uint ProgressIndex { get; }
    public ulong ProgressHash { get; }

    public QuestContext(uint category, uint subCategory, uint index, uint progressIndex, ulong progressHash)
    {
        Category = category;
        SubCategory = subCategory;
        Index = index;
        ProgressIndex = progressIndex;
        ProgressHash = progressHash;
    }

    public uint GetQuestId()
    {
         return (Category << 20) | (SubCategory << 12) | Index;
    }
}
