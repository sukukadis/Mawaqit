using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media;
using Mawaqit.Models;

namespace Mawaqit.Services;

public class AudioService
{
    private readonly MediaPlayer _player = new();
    private readonly object _lock = new();

    private string? _currentPath;
    private bool _isTestPlayback;
    private bool _isPlaying;

    public event Action? PlaybackStarted;
    public event Action? PlaybackStopped;

    public bool IsPlaying => _isPlaying;
    public string? CurrentPlayingPath => _currentPath;
    public bool IsTestPlayback => _isTestPlayback;

    public AudioService()
    {
        _player.MediaEnded += OnMediaEnded;
        _player.MediaFailed += OnMediaFailed;
    }

    private void OnMediaEnded(object? sender, EventArgs e)
    {
        lock (_lock)
        {
            _isPlaying = false;
            _isTestPlayback = false;
            _currentPath = null;
        }
        PlaybackStopped?.Invoke();
    }

    private void OnMediaFailed(object? sender, ExceptionEventArgs e)
    {
        lock (_lock)
        {
            _isPlaying = false;
            _isTestPlayback = false;
            _currentPath = null;
        }
        Logger.LogError($"Audio playback failed: {e.ErrorException?.Message}");
        PlaybackStopped?.Invoke();
    }

    public static List<string> GetAudioSearchDirs()
    {
        var dirs = new List<string>
        {
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Media"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources"),
            AppDomain.CurrentDomain.BaseDirectory,
            AppConfig.AppDataFolder,
            Environment.CurrentDirectory
        };
        return dirs;
    }

    public static string ResolveAudioFile(string? storedPath, string filename)
    {
        // 1. Clean up legacy Roaming\Media folder if left over from older versions
        try
        {
            if (Directory.Exists(AppConfig.MediaFolder))
            {
                Directory.Delete(AppConfig.MediaFolder, true);
            }
        }
        catch { }

        // 2. If storedPath points to a valid file outside the legacy Roaming Media folder, keep it (e.g. user custom audio)
        if (!string.IsNullOrWhiteSpace(storedPath) &&
            !storedPath.StartsWith(AppConfig.MediaFolder, StringComparison.OrdinalIgnoreCase) &&
            File.Exists(storedPath))
        {
            return storedPath;
        }

        // 3. Search application installation directories directly without copying to Roaming
        foreach (var dir in GetAudioSearchDirs())
        {
            if (!Directory.Exists(dir)) continue;
            string cand = Path.Combine(dir, filename);
            if (File.Exists(cand))
            {
                return cand;
            }
        }

        // 4. Fallback if storedPath still exists anywhere
        if (!string.IsNullOrWhiteSpace(storedPath) && File.Exists(storedPath))
        {
            return storedPath;
        }

        return "";
    }

    public bool Play(string path, double volume = 0.5, bool isMuted = false, bool isTest = false)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            Logger.LogError($"Audio file missing: {path}");
            return false;
        }

        try
        {
            Stop();

            lock (_lock)
            {
                _currentPath = path;
                _isTestPlayback = isTest;
                _isPlaying = true;
            }

            _player.Open(new Uri(Path.GetFullPath(path), UriKind.Absolute));
            _player.Volume = Math.Clamp(volume, 0.0, 1.0);
            _player.IsMuted = isMuted;
            _player.Play();

            PlaybackStarted?.Invoke();
            return true;
        }
        catch (Exception ex)
        {
            lock (_lock)
            {
                _isPlaying = false;
                _isTestPlayback = false;
                _currentPath = null;
            }
            Logger.LogError($"AudioService.Play exception: {ex.Message}");
            return false;
        }
    }

    public void Stop()
    {
        try
        {
            _player.Stop();
            _player.Close();
        }
        catch { }

        lock (_lock)
        {
            _isPlaying = false;
            _isTestPlayback = false;
            _currentPath = null;
        }

        PlaybackStopped?.Invoke();
    }

    public void SetVolume(double volume)
    {
        try
        {
            _player.Volume = Math.Clamp(volume, 0.0, 1.0);
        }
        catch { }
    }

    public void SetMute(bool isMuted)
    {
        try
        {
            _player.IsMuted = isMuted;
        }
        catch { }
    }
}
