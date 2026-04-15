using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using PortKiller.Models;
using PortKiller.ViewModels;
using PortKiller.Helpers;

namespace PortKiller;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly TunnelViewModel _tunnelViewModel;
    private Hardcodet.Wpf.TaskbarNotification.TaskbarIcon? _trayIcon;
    private bool _isShuttingDown = false;

    public MainWindow()
    {
        InitializeComponent();

        _viewModel = App.Services.GetRequiredService<MainViewModel>();
        DataContext = _viewModel;
        _tunnelViewModel = App.Services.GetRequiredService<TunnelViewModel>();
        TunnelProtocolCombo.DataContext = _tunnelViewModel;
        InitializeAsync();
        
        // Setup keyboard shortcuts
        SetupKeyboardShortcuts();
        
        // Initialize system tray icon
        InitializeTrayIcon();
        
        // Ensure window is visible and activated on startup
    }

    private void InitializeTrayIcon()
    {
        _trayIcon = new Hardcodet.Wpf.TaskbarNotification.TaskbarIcon
        {
            ToolTipText = $"PortKiller {AppVersionInfo.DisplayVersion} — arpcodes.com",
            Visibility = Visibility.Visible
        };

        _trayIcon.Icon = LoadTrayIcon();

        var miStyle = Application.Current.TryFindResource("TrayMenuItemStyle") as Style;
        var sepStyle = Application.Current.TryFindResource("TraySeparatorStyle") as Style;
        var trayMenuStyle = Application.Current.TryFindResource("TrayContextMenuStyle") as Style;
        var contextMenu = new ContextMenu();
        if (trayMenuStyle != null)
            contextMenu.Style = trayMenuStyle;
        else
        {
            contextMenu.Background = Application.Current.TryFindResource("TrayMenuBackgroundBrush") as System.Windows.Media.Brush
                ?? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 48));
            contextMenu.BorderBrush = Application.Current.TryFindResource("TrayMenuBorderBrush") as System.Windows.Media.Brush
                ?? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(63, 63, 70));
            contextMenu.BorderThickness = new Thickness(1);
            contextMenu.Padding = new Thickness(4);
            contextMenu.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(232, 232, 232));
        }

        var openItem = new MenuItem { Header = "Open main window", FontWeight = FontWeights.SemiBold };
        ApplyTrayMenuItemChrome(openItem, miStyle);
        openItem.Click += TrayOpenMain_Click;
        contextMenu.Items.Add(openItem);

        contextMenu.Items.Add(CreateTraySeparator(sepStyle));

        var refreshItem = new MenuItem { Header = "Refresh", InputGestureText = "Ctrl+R" };
        ApplyTrayMenuItemChrome(refreshItem, miStyle);
        refreshItem.Click += TrayRefresh_Click;
        contextMenu.Items.Add(refreshItem);

        var killAllItem = new MenuItem { Header = "Kill all", InputGestureText = "Ctrl+K" };
        ApplyTrayMenuItemChrome(killAllItem, miStyle);
        killAllItem.Click += TrayKillAll_Click;
        contextMenu.Items.Add(killAllItem);

        contextMenu.Items.Add(CreateTraySeparator(sepStyle));

        var settingsItem = new MenuItem { Header = "Settings" };
        ApplyTrayMenuItemChrome(settingsItem, miStyle);
        settingsItem.Click += TraySettings_Click;
        contextMenu.Items.Add(settingsItem);

        var quitItem = new MenuItem { Header = "Quit", InputGestureText = "Ctrl+Q" };
        ApplyTrayMenuItemChrome(quitItem, miStyle);
        quitItem.Click += TrayQuit_Click;
        contextMenu.Items.Add(quitItem);

        _trayIcon.ContextMenu = contextMenu;
        _trayIcon.TrayLeftMouseDown += TrayIcon_Click;
    }

    private static Separator CreateTraySeparator(Style? sepStyle)
    {
        var s = new Separator();
        if (sepStyle != null) s.Style = sepStyle;
        return s;
    }

    /// <summary>System MenuText brush stays dark on our dark menu unless Foreground is explicit; tray popup may not inherit window resources.</summary>
    private static void ApplyTrayMenuItemChrome(MenuItem item, Style? miStyle)
    {
        if (miStyle != null)
            item.Style = miStyle;
        item.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(232, 232, 232));
    }

    private static System.Drawing.Icon LoadTrayIcon()
    {
        var uri = new Uri("pack://application:,,,/Assets/app.ico", UriKind.Absolute);
        var info = Application.GetResourceStream(uri)
            ?? throw new InvalidOperationException("Missing embedded Assets/app.ico.");
        // Copy to memory: resource stream may not seek; Icon(Stream,int,int) picks best embedded size for shell scaling.
        using (var src = info.Stream)
        using (var ms = new System.IO.MemoryStream())
        {
            src.CopyTo(ms);
            ms.Position = 0;
            // Prefer 32×32 frame so Windows scales down crisply to 16/20/24px tray slots (multi-size .ico from IconGen).
            return new System.Drawing.Icon(ms, 32, 32);
        }
    }

    private async void InitializeAsync()
    {
        await _viewModel.InitializeAsync();
        UpdateUI();

        // Subscribe to property changes
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(_viewModel.FilteredPorts) ||
                e.PropertyName == nameof(_viewModel.IsScanning))
            {
                Dispatcher.Invoke(UpdateUI);
            }
            
            if (e.PropertyName == nameof(_viewModel.Ports))
            {
                Dispatcher.Invoke(UpdateTrayMenu);
            }
        };
    }

    private void UpdateUI()
    {
        // Update ports list
        PortsListView.ItemsSource = _viewModel.FilteredPorts;

        // Update empty state
        EmptyState.Visibility = _viewModel.FilteredPorts.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        // Update status
        StatusText.Text = _viewModel.IsScanning
            ? "Scanning ports..."
            : $"{_viewModel.FilteredPorts.Count} port(s) listening";
    }

    // Window Controls
    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }
        else
        {
            DragMove();
        }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.RefreshPortsCommand.ExecuteAsync(null);
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _viewModel.Search(SearchBox.Text);
    }

    private void SidebarButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string tag)
        {
            if (Enum.TryParse<SidebarItem>(tag, out var sidebarItem))
            {
                _viewModel.SelectedSidebarItem = sidebarItem;
                HeaderText.Text = sidebarItem.GetTitle();
                
                if (sidebarItem == SidebarItem.CloudflareTunnels)
                {
                    SettingsPanel.Visibility = Visibility.Collapsed;
                    PortsPanel.Visibility = Visibility.Collapsed;
                    DetailPanel.Visibility = Visibility.Collapsed;
                    TunnelsPanel.Visibility = Visibility.Visible;
                    UpdateTunnelsUI();
                }
                else if (sidebarItem == SidebarItem.Settings)
                {
                    SettingsPanel.Visibility = Visibility.Visible;
                    PortsPanel.Visibility = Visibility.Collapsed;
                    DetailPanel.Visibility = Visibility.Collapsed;
                    TunnelsPanel.Visibility = Visibility.Collapsed;
                }
                else
                {
                    SettingsPanel.Visibility = Visibility.Collapsed;
                    TunnelsPanel.Visibility = Visibility.Collapsed;
                    PortsPanel.Visibility = Visibility.Visible;
                }
                
                // Highlight selected button (optional enhancement)
                foreach (var child in ((button.Parent as Panel)?.Children ?? new UIElementCollection(null, null)))
                {
                    if (child is Button btn)
                    {
                        btn.Background = System.Windows.Media.Brushes.Transparent;
                    }
                }
                button.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(25, 52, 152, 219));
            }
        }
    }

    private void PortItem_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.Tag is PortInfo port)
        {
            _viewModel.SelectedPort = port;
            ShowPortDetails(port);
        }
    }

    private void ShowPortDetails(PortInfo port)
    {
        DetailPanel.Visibility = Visibility.Visible;

        DetailPort.Text = port.DisplayPort;
        DetailProcess.Text = port.ProcessName;
        DetailPid.Text = port.Pid.ToString();
        DetailAddress.Text = port.Address;
        DetailUser.Text = port.User;
        DetailCommand.Text = port.Command;

        // Update favorite button
        FavoriteButton.Content = _viewModel.IsFavorite(port.Port)
            ? "⭐ Remove from Favorites"
            : "⭐ Add to Favorites";

        // Update watch button
        WatchButton.Content = _viewModel.IsWatched(port.Port)
            ? "👁 Unwatch Port"
            : "👁 Watch Port";
    }

    private async void KillButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is PortInfo port)
        {
            var dialog = new ConfirmDialog(
                $"Are you sure you want to kill the process on port {port.Port}?",
                $"Process: {port.ProcessName}\nPID: {port.Pid}\n\nThis action cannot be undone.",
                "Kill Process")
            {
                Owner = this
            };
            
            dialog.ShowDialog();

            if (dialog.Result)
            {
                await _viewModel.KillProcessCommand.ExecuteAsync(port);
            }
        }
    }

    private void FavoriteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedPort != null)
        {
            _viewModel.ToggleFavoriteCommand.Execute(_viewModel.SelectedPort.Port);
            ShowPortDetails(_viewModel.SelectedPort);
        }
    }

    private void WatchButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedPort != null)
        {
            var port = _viewModel.SelectedPort.Port;

            if (_viewModel.IsWatched(port))
            {
                _viewModel.RemoveWatchedPortCommand.Execute(port);
            }
            else
            {
                _viewModel.AddWatchedPortCommand.Execute(port);
            }

            ShowPortDetails(_viewModel.SelectedPort);
        }
    }

    private async void ShareTunnelButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedPort == null)
            return;

        var port = _viewModel.SelectedPort.Port;

        _viewModel.SelectedSidebarItem = SidebarItem.CloudflareTunnels;
        HeaderText.Text = SidebarItem.CloudflareTunnels.GetTitle();

        SettingsPanel.Visibility = Visibility.Collapsed;
        PortsPanel.Visibility = Visibility.Collapsed;
        DetailPanel.Visibility = Visibility.Collapsed;
        TunnelsPanel.Visibility = Visibility.Visible;
        UpdateTunnelsUI();

        // Start tunnel for the selected port
        await _tunnelViewModel.StartTunnelAsync(port);
    }

    // Window loaded event - enable blur for sidebar only
    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (_viewModel.StartMinimizedToTray)
        {
            Hide();
        }
        else
        {
            Show();
            Activate();
            WindowState = WindowState.Normal;
        }

        try
        {
            WindowBlurHelper.EnableAcrylicBlur(this, blurOpacity: 180, blurColor: 0x1A1A1A);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Blur effect not supported: {ex.Message}");
        }
    }

    // Keyboard shortcuts
    private void SetupKeyboardShortcuts()
    {
        var refreshGesture = new KeyGesture(Key.R, ModifierKeys.Control);
        var killAllGesture = new KeyGesture(Key.K, ModifierKeys.Control);
        var quitGesture = new KeyGesture(Key.Q, ModifierKeys.Control);

        InputBindings.Add(new KeyBinding(_viewModel.RefreshPortsCommand, refreshGesture));
        InputBindings.Add(new KeyBinding(ApplicationCommands.Close, quitGesture));
        
        CommandBindings.Add(new CommandBinding(ApplicationCommands.Close, (s, e) => Close()));
    }

    // System tray icon handlers
    private void TrayIcon_Click(object sender, RoutedEventArgs e)
    {
        // Show mini popup window near tray
        var miniWindow = new MiniPortKillerWindow();
        miniWindow.ShowNearTray();
    }

    private async void TrayRefresh_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.RefreshPortsCommand.ExecuteAsync(null);
    }

    private async void TrayKillAll_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ConfirmDialog(
            "Are you sure you want to kill ALL processes on listening ports?",
            $"This will terminate {_viewModel.Ports.Count} process(es).\n\nThis action cannot be undone.",
            "Kill All Processes")
        {
            Owner = this
        };
        
        dialog.ShowDialog();

        if (dialog.Result)
        {
            foreach (var port in _viewModel.Ports.ToList())
            {
                try
                {
                    await _viewModel.KillProcessCommand.ExecuteAsync(port);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to kill process on port {port.Port}: {ex.Message}");
                }
            }
        }
    }

    // Cloudflare Tunnels Methods
    private void UpdateTunnelsUI()
    {
        // Bind tunnels list to view model
        TunnelsListView.ItemsSource = _tunnelViewModel.Tunnels;

        UpdateTunnelsContentVisibility();

        // Update status bar
        var count = _tunnelViewModel.ActiveTunnelCount;
        TunnelStatusText.Text = $"{count} active tunnel(s)";
        TunnelStatusDot.Fill = count > 0
            ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 204, 113))
            : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(128, 128, 128));

        // Update Stop All button visibility
        StopAllTunnelsButton.Visibility = _tunnelViewModel.Tunnels.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

        // Subscribe to collection changes if not already
        _tunnelViewModel.Tunnels.CollectionChanged -= OnTunnelsCollectionChanged;
        _tunnelViewModel.Tunnels.CollectionChanged += OnTunnelsCollectionChanged;
    }

    /// <summary>
    /// Keeps the cloudflared banner, tunnel list, and empty state in separate layout slots so they never overlap.
    /// Empty state is only shown when cloudflared is installed and there are no tunnels.
    /// </summary>
    private void UpdateTunnelsContentVisibility()
    {
        var installed = _tunnelViewModel.IsCloudflaredInstalled;
        var hasTunnels = _tunnelViewModel.Tunnels.Count > 0;

        CloudflaredWarning.Visibility = installed ? Visibility.Collapsed : Visibility.Visible;
        CloudflaredInstalledIndicator.Visibility = installed ? Visibility.Visible : Visibility.Collapsed;

        TunnelsEmptyState.Visibility = installed && !hasTunnels ? Visibility.Visible : Visibility.Collapsed;
        TunnelsListScrollViewer.Visibility = hasTunnels ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnTunnelsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            UpdateTunnelsContentVisibility();

            var count = _tunnelViewModel.ActiveTunnelCount;
            TunnelStatusText.Text = $"{count} active tunnel(s)";
            TunnelStatusDot.Fill = count > 0
                ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 204, 113))
                : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(128, 128, 128));

            StopAllTunnelsButton.Visibility = _tunnelViewModel.Tunnels.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        });
    }

    private void RefreshTunnels_Click(object sender, RoutedEventArgs e)
    {
        _tunnelViewModel.RecheckInstallation();
        UpdateTunnelsUI();
    }

    private async void StopAllTunnels_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ConfirmDialog(
            $"Are you sure you want to stop all {_tunnelViewModel.Tunnels.Count} tunnel(s)?",
            "All public URLs will be terminated immediately.\n\nThis action cannot be undone.",
            "Stop All Tunnels")
        {
            Owner = this
        };
        
        dialog.ShowDialog();

        if (dialog.Result)
        {
            await _tunnelViewModel.StopAllTunnelsAsync();
            UpdateTunnelsUI();
        }
    }

    private async void TunnelStop_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: CloudflareTunnel tunnel })
            return;

        await _tunnelViewModel.StopTunnelAsync(tunnel);
    }

    private void TunnelCopy_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: CloudflareTunnel tunnel })
            return;

        if (!string.IsNullOrEmpty(tunnel.TunnelUrl))
        {
            _tunnelViewModel.CopyUrlToClipboard(tunnel.TunnelUrl);
        }
    }

    private void TunnelOpen_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: CloudflareTunnel tunnel })
            return;

        if (!string.IsNullOrEmpty(tunnel.TunnelUrl))
        {
            _tunnelViewModel.OpenUrlInBrowser(tunnel.TunnelUrl);
        }
    }

    private void TrayOpenMain_Click(object sender, RoutedEventArgs e)
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void TraySettings_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.SelectedSidebarItem = SidebarItem.Settings;
        HeaderText.Text = "Settings";
        SettingsPanel.Visibility = Visibility.Visible;
        PortsPanel.Visibility = Visibility.Collapsed;
        DetailPanel.Visibility = Visibility.Collapsed;
        TunnelsPanel.Visibility = Visibility.Collapsed;
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void TrayQuit_Click(object sender, RoutedEventArgs e)
    {
        _isShuttingDown = true;
        Application.Current.Shutdown();
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (!_isShuttingDown)
        {
            if (_viewModel.CloseToTray)
            {
                e.Cancel = true;
                Hide();
            }
            else
            {
                // Quit: allow close to proceed
            }
        }
        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        _trayIcon?.Dispose();
        base.OnClosed(e);
    }

    // Update tray menu with active ports
    private void UpdateTrayMenu()
    {
        if (_trayIcon == null || _trayIcon.ContextMenu == null) return;

        var contextMenu = _trayIcon.ContextMenu;
        var miStyle = Application.Current.TryFindResource("TrayMenuItemStyle") as Style;
        
        // Remove old port menu items (everything before first separator)
        var firstSeparatorIndex = contextMenu.Items.Cast<object>()
            .TakeWhile(item => item is not Separator)
            .Count();
        
        // Remove old port items
        for (int i = contextMenu.Items.Count - 1; i >= 0; i--)
        {
            if (contextMenu.Items[i] is MenuItem menuItem && menuItem.Tag is PortInfo)
            {
                contextMenu.Items.RemoveAt(i);
            }
        }

        // Add header if there are ports
        var ports = _viewModel.Ports.Take(10).ToList(); // Limit to 10 ports
        
        if (ports.Any())
        {
            var separatorIndex = -1;
            for (int i = 0; i < contextMenu.Items.Count; i++)
            {
                if (contextMenu.Items[i] is Separator)
                {
                    separatorIndex = i;
                    break;
                }
            }

            if (separatorIndex > 0)
            {
                int insertIndex = 1;
                foreach (var port in ports)
                {
                    var portMenuItem = new MenuItem
                    {
                        Header = $"● :{port.Port}  {port.ProcessName} (PID: {port.Pid})",
                        Tag = port
                    };
                    ApplyTrayMenuItemChrome(portMenuItem, miStyle);
                    portMenuItem.Click += async (s, e) =>
                    {
                        var menuItem = s as MenuItem;
                        if (menuItem?.Tag is PortInfo portInfo)
                        {
                            var dialog = new ConfirmDialog(
                                $"Kill process on port {portInfo.Port}?",
                                $"Process: {portInfo.ProcessName}\nPID: {portInfo.Pid}",
                                "Kill Process")
                            {
                                Owner = this
                            };
                            
                            dialog.ShowDialog();

                            if (dialog.Result)
                            {
                                await _viewModel.KillProcessCommand.ExecuteAsync(portInfo);
                            }
                        }
                    };
                    contextMenu.Items.Insert(insertIndex++, portMenuItem);
                }
            }
        }
    }
}
