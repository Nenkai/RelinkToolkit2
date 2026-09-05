using RelinkToolkit2.ViewModels.Documents.GraphEditor.Nodes;
using RelinkToolkit2.ViewModels.Search;

namespace RelinkToolkit2.Messages.Documents;

public class OpenComponentSearchFlyoutRequest
{
    public NodeViewModel Node { get; }
    public ComponentSearchViewModel SearchViewModel { get; }

    public OpenComponentSearchFlyoutRequest(NodeViewModel node, ComponentSearchViewModel searchViewModel)
    {
        Node = node;
        SearchViewModel = searchViewModel;
    }
}
