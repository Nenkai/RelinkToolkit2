using RelinkToolkit2.ViewModels.Documents.GraphEditor.Nodes;
using RelinkToolkit2.ViewModels.Search;

namespace RelinkToolkit2.Messages.Documents;

public class CopyNodeGuidRequest
{
    public NodeViewModel Node { get; }

    public CopyNodeGuidRequest(NodeViewModel node)
    {
        Node = node;
    }
}
