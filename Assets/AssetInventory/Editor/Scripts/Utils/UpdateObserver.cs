using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace AssetInventory
{
    public sealed class UpdateObserver
    {
        public bool InitializationDone;
        public float InitializationProgress;
        public bool PrioInitializationDone;
        public float PrioInitializationProgress;
        public int DownloadCount;
        
        private readonly FileSystemWatcher _watcher;
        private readonly string[] _fileTypes;
        
        private List<AssetInfo> _all = new List<AssetInfo>();
        private List<AssetInfo> _prioritized;
        private readonly Dictionary<int, AssetDownloader> _loaders = new Dictionary<int, AssetDownloader>();
        
        private int _prioCount;
        private int _curIndex;
        
        public UpdateObserver(string path, IEnumerable<string> fileTypes)
        {
            if (!Directory.Exists(path)) return; // will throw error otherwise
            
            _fileTypes = fileTypes.Select(ft => "." + ft).ToArray(); // ensure fileTypes include the dot prefix
            
            _watcher = new FileSystemWatcher();
            _watcher.Path = path;
            _watcher.IncludeSubdirectories = true;
            _watcher.Filter = "*.*";
            _watcher.InternalBufferSize = 65536;
            
            _watcher.NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite;
            
            _watcher.Changed += OnChanged;
            _watcher.Created += OnCreated;
            _watcher.Deleted += OnDeleted;
            _watcher.Renamed += OnRenamed;
            _watcher.Error += (_, args) => { Debug.LogWarning($"Asset cache monitoring error: {args.GetException()}"); };
            
            ScanContinuously();
        }
        
        private async void ScanContinuously()
        {
            while (true)
            {
                if (_all == null || _all.Count == 0)
                {
                    await Task.Delay(2000);
                    continue;
                }
                
                // refresh currently downloading items faster to show progress bars
                DownloadCount = 0;
                for (int i = 0; i < _all.Count; i++)
                {
                    AssetInfo download = _all[i];
                    if (download.PackageDownloader.GetState().state != AssetDownloader.State.Downloading)
                    {
                        // always refresh for single selections
                        if (_prioCount == 1 && i == 0) download.PackageDownloader.RefreshState();
                        continue;
                    }
                    
                    DownloadCount++;
                    download.PackageDownloader.RefreshState();
                    await Task.Delay(10);
                }
                
                if (_curIndex >= _prioCount) PrioInitializationDone = true;
                if (_curIndex >= _all.Count)
                {
                    InitializationDone = true;
                    await Task.Delay(1000);
                    continue;
                }
                
                AssetInfo info = _all[_curIndex];
                
                // refresh prioritized items faster
                if (DateTime.Now - info.PackageDownloader.lastRefresh > TimeSpan.FromSeconds(_curIndex < _prioCount ? 5 : 30))
                {
                    info.Refresh();
                    info.PackageDownloader.RefreshState();
                    if (_curIndex % AssetInventory.Config.observationSpeed == 0) await Task.Yield();
                }
                
                InitializationProgress = (float)_curIndex / _all.Count;
                PrioInitializationProgress = (float)_curIndex / _prioCount;
                _curIndex++;
            }
        }
        
        public void SetPrioritized(List<AssetInfo> prioritized)
        {
            // skip setting the same list twice since that will reset the initialization state
            if (_prioritized != null && _prioritized.SequenceEqual(prioritized)) return;
            
            // sort prioritized to the beginning of all
            // below two lines are nicer to read but much slower than using a hashset + recreate
            // _all.RemoveAll(prioritized.Contains);
            // _all.InsertRange(0, prioritized);
            
            _prioritized = prioritized.OrderBy(info => info.PackageDownloader == null ? DateTime.MinValue : info.PackageDownloader.lastRefresh).ToList(); // break reference
            _prioCount = _prioritized.Count;
            
            // single items will get refreshed automatically, bulk selections need a rescan 
            if (_prioCount > 1)
            {
                InitializationDone = false;
                InitializationProgress = 0;
                PrioInitializationDone = false;
                PrioInitializationProgress = 0;
                _curIndex = 0;
            }
            
            // Convert prioritized to a HashSet for faster lookups
            HashSet<AssetInfo> prioritizedSet = new HashSet<AssetInfo>(prioritized);
            
            // Create a new list to hold the re-ordered items
            List<AssetInfo> reordered = new List<AssetInfo>(prioritized);
            
            // Add non-prioritized items to the reordered list, skipping those in prioritized
            foreach (AssetInfo item in _all)
            {
                if (!prioritizedSet.Contains(item)) reordered.Add(item);
            }
            _all = reordered;
            
            AttachDownloaders();
        }
        
        public void SetAll(List<AssetInfo> all)
        {
            _curIndex = 0;
            _all = all;
            AttachDownloaders();
        }
        
        private void AttachDownloaders()
        {
            _all.ForEach(Attach);
        }
        
        public void Attach(AssetInfo info)
        {
            if (info.PackageDownloader == null)
            {
                // hook up existing downloads if existent
                if (_loaders.TryGetValue(info.AssetId, out AssetDownloader downloader))
                {
                    info.PackageDownloader = downloader;
                }
                else
                {
                    info.PackageDownloader = new AssetDownloader(info);
                    _loaders.Add(info.AssetId, info.PackageDownloader);
                }
            }
            
            // update reference in case new data was added
            info.PackageDownloader.SetAsset(info);
        }
        
        public void SetPath(string path)
        {
            _watcher.Path = path;
        }
        
        private bool IsWatchedType(string path)
        {
            return _fileTypes.Any(ft => path.EndsWith(ft, StringComparison.OrdinalIgnoreCase));
        }
        
        private void TriggerRefresh(string path)
        {
            // if directory then use directly, otherwise use the directory
            string lowerPath = path.ToLowerInvariant();
            if (Path.GetExtension(lowerPath) != "") lowerPath = Path.GetDirectoryName(lowerPath);
            
            // refresh all affected files
            _all
                .Where(info => !string.IsNullOrEmpty(info.Location))
                .Where(info => info.GetLocation(true).ToLowerInvariant().StartsWith(lowerPath)).ForEach(info =>
                {
                    info.Refresh();
                    info.PackageDownloader?.RefreshState();
                });
        }
        
        private void OnCreated(object source, FileSystemEventArgs e)
        {
            // Debug.Log($"Created File: {e.FullPath} {e.ChangeType}");
            
            TriggerRefresh(e.FullPath);
        }
        
        private void OnChanged(object source, FileSystemEventArgs e)
        {
            // Debug.Log($"Changed File: {e.FullPath} {e.ChangeType}");
            
            TriggerRefresh(e.FullPath);
        }
        
        private void OnDeleted(object source, FileSystemEventArgs e)
        {
            // Debug.Log($"Deleted File: {e.FullPath} {e.ChangeType}");
            
            TriggerRefresh(e.FullPath);
        }
        
        private void OnRenamed(object source, RenamedEventArgs e)
        {
            // Debug.Log($"File: {e.OldFullPath} renamed to {e.FullPath}");
            
            TriggerRefresh(e.FullPath);
        }
        
        public async void Start()
        {
            // enabling the events will scan the directory which can lock up the main thread
            await Task.Run(() =>
            {
                if (!string.IsNullOrEmpty(_watcher?.Path)) _watcher.EnableRaisingEvents = true;
            });
        }
        
        public async void Stop()
        {
            await Task.Run(() =>
            {
                if (_watcher != null && _watcher.EnableRaisingEvents) _watcher.EnableRaisingEvents = false;
            });
        }
    }
}
