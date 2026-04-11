namespace KubeNavigator.ViewModels.Navigation;

public interface INavigationGroupIcon;

public record SymbolNavigationGroupIcon(string Glyph) : INavigationGroupIcon;

public record PathNavigationGroupIcon(string PathData) : INavigationGroupIcon;
