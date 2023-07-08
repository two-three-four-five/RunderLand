/******************************************************************************
 * File: SpatialAnchorStoreManager.cs
 * Copyright (c) 2022 Qualcomm Technologies, Inc. and/or its subsidiaries. All rights reserved.
 *
 * Confidential and Proprietary - Qualcomm Technologies, Inc.
 *
 ******************************************************************************/

using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.SubsystemsImplementation.Extensions;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.OpenXR;

namespace Qualcomm.Snapdragon.Spaces
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ARAnchorManager))]
    public class SpacesAnchorStore : MonoBehaviour
    {
        private class SaveAnchorData
        {
            public readonly ARAnchor Anchor;
            public readonly string AnchorName;
            public readonly Action<bool> OnSavedCallback;
            public bool Success;

            // false = 0; true = 1
            public int UsingResource;

            public SaveAnchorData(ARAnchor anchor, string anchorName, Action<bool> onSavedCallback)
            {
                Anchor = anchor;
                AnchorName = anchorName;
                OnSavedCallback = onSavedCallback;
                Success = false;
                UsingResource = 0;
            }
        }

        private class LoadAnchorData
        {
            public readonly string AnchorName;
            public readonly Action<bool> OnLoadedCallback;
            public ulong AnchorHandle;
            public ulong SpaceHandle;
            public bool Success;

            // false = 0; true = 1
            public int UsingResource;

            public LoadAnchorData(string anchorName, Action<bool> onLoadedCallback)
            {
                AnchorName = anchorName;
                AnchorHandle = 0;
                SpaceHandle = 0;
                OnLoadedCallback = onLoadedCallback;
                Success = false;
                UsingResource = 0;
            }
        }

        private readonly Queue<SaveAnchorData> _saveAnchorPendingQueue = new Queue<SaveAnchorData>();
        private readonly Queue<LoadAnchorData> _loadAnchorPendingQueue = new Queue<LoadAnchorData>();
        private SpatialAnchorsFeature _feature;
        private SpatialAnchorsSubsystem _subsystem;
        private ulong _spatialAnchorStore;
        private bool _isStoreLoaded;

        // false = 0; true = 1
        private int _saveAnchorUsingResource;
        private SaveAnchorData _lastSaveAnchorData;

        // false = 0; true = 1
        private int _loadAnchorUsingResource;
        private LoadAnchorData _lastLoadAnchorData;

        public void ClearStore()
        {
            if (_isStoreLoaded && _feature.TryClearSpatialAnchorStoreMSFT(_spatialAnchorStore))
            {
                var provider = (SpatialAnchorsSubsystem.SpatialAnchorsProvider)_subsystem.GetProvider();
                provider.ClearAllAnchorSavedNames();
            }
        }

        public void SaveAnchor(ARAnchor anchor, string anchorName, Action<bool> onSavedCallback = null)
        {
            if (_isStoreLoaded)
            {
                _saveAnchorPendingQueue.Enqueue(new SaveAnchorData(anchor, anchorName, onSavedCallback));
            }
            else
            {
                onSavedCallback?.Invoke(false);
            }
        }

        public void SaveAnchor(ARAnchor anchor, Action<bool> onSavedCallback = null)
        {
            int hashCode = anchor.trackableId.GetHashCode();
            hashCode = (hashCode * 4999559) + DateTime.Now.GetHashCode();
            SaveAnchor(anchor, hashCode.ToString(), onSavedCallback);
        }

        [Obsolete]
        public void SaveAnchor(ARAnchor anchor, string anchorName)
        {
            if (_isStoreLoaded)
            {
                ulong anchorHandle = anchor.trackableId.subId1;
                if (_feature.TryPersistSpatialAnchor(_spatialAnchorStore, anchorHandle, anchorName))
                {
                    var provider = (SpatialAnchorsSubsystem.SpatialAnchorsProvider)_subsystem.GetProvider();
                    provider.UpdateAnchorSavedName(anchor.trackableId, anchorName);
                }
            }
        }

        public void DeleteSavedAnchor(string anchorName)
        {
            if (!_isStoreLoaded)
            {
                return;
            }

            if (anchorName == string.Empty)
            {
                Debug.LogError("Can't delete an anchor with an empty name.");
                return;
            }

            _feature.TryUnpersistSpatialAnchor(_spatialAnchorStore, anchorName);
        }

        public void LoadSavedAnchor(string anchorName, Action<bool> onLoadedCallback = null)
        {
            if (!_isStoreLoaded)
            {
                onLoadedCallback?.Invoke(false);
                return;
            }

            if (anchorName == string.Empty)
            {
                Debug.LogError("Can't create an anchor with an empty name.");
                onLoadedCallback?.Invoke(false);
                return;
            }

            _loadAnchorPendingQueue.Enqueue(new LoadAnchorData(anchorName, onLoadedCallback));
        }

        [Obsolete]
        public bool LoadSavedAnchor(string anchorName)
        {
            if (!_isStoreLoaded)
            {
                return false;
            }

            if (anchorName == string.Empty)
            {
                Debug.LogError("Can't create an anchor with an empty name.");
                return false;
            }

            GameObject go = new GameObject
            {
                name = anchorName,
                transform =
                {
                    position = Vector3.zero,
                    rotation = Quaternion.identity
                }
            };
            go.SetActive(false);
            var provider = (SpatialAnchorsSubsystem.SpatialAnchorsProvider)_subsystem.GetProvider();
            if (!provider.TryAddAnchorFromPersistentName(_spatialAnchorStore, anchorName))
            {
                Destroy(go);
                return false;
            }

            go.AddComponent<ARAnchor>();
            go.SetActive(true);
            return true;
        }

        public void LoadSavedAnchorAsync(object loadAnchorData)
        {
            LoadAnchorData data = (LoadAnchorData)loadAnchorData;
            if (_feature.TryCreateSpatialAnchorFromPersistedNameMSFT(_spatialAnchorStore, data.AnchorName, out ulong spatialAnchorHandle))
            {
                ulong anchorSpaceHandle = _feature.TryCreateSpatialAnchorSpaceHandle(spatialAnchorHandle);
                if (anchorSpaceHandle != 0)
                {
                    data.AnchorHandle = spatialAnchorHandle;
                    data.SpaceHandle = anchorSpaceHandle;
                    data.Success = true;
                }
                else
                {
                    data.Success = false;
                }
            }

            Interlocked.Exchange(ref data.UsingResource, 0);
            Interlocked.Exchange(ref _loadAnchorUsingResource, 0);
        }

        public void LoadAllSavedAnchors(Action<bool> onLoadedCallback = null)
        {
            if (!_isStoreLoaded)
            {
                return;
            }

            string[] anchorNames = GetSavedAnchorNames();
            foreach (var anchorName in anchorNames)
            {
                LoadSavedAnchor(anchorName, onLoadedCallback);
            }
        }

        public string[] GetSavedAnchorNames()
        {
            if (!_isStoreLoaded)
            {
                return Array.Empty<string>();
            }

            _feature.TryEnumeratePersistedSpatialAnchorNames(_spatialAnchorStore, out string[] namesList);
            return namesList;
        }

        public string GetSavedAnchorNameFromARAnchor(ARAnchor anchor)
        {
            if (!_isStoreLoaded)
            {
                return string.Empty;
            }

            var provider = (SpatialAnchorsSubsystem.SpatialAnchorsProvider)_subsystem.GetProvider();
            return provider.TryGetSavedNameFromTrackableId(anchor.trackableId);
        }

        private void Awake()
        {
            _feature = OpenXRSettings.Instance.GetFeature<SpatialAnchorsFeature>();
            var subsystems = new List<SpatialAnchorsSubsystem>();
            SubsystemManager.GetInstances(subsystems);
            if (subsystems.Count > 0)
            {
                _subsystem = subsystems[0];
            }
            else
            {
                Debug.LogError("Failed to get SpatialAnchorsSubsystem instance. Aborting SpacesSpatialAnchorStore initialization!");
                return;
            }

            LoadStore();
        }

        private void Update()
        {
            if (_lastSaveAnchorData != null && Interlocked.Exchange(ref _lastSaveAnchorData.UsingResource, 1) == 0)
            {
                if (_lastSaveAnchorData.Success)
                {
                    var provider = (SpatialAnchorsSubsystem.SpatialAnchorsProvider)_subsystem.GetProvider();
                    provider.UpdateAnchorSavedName(_lastSaveAnchorData.Anchor.trackableId, _lastSaveAnchorData.AnchorName);
                }

                _lastSaveAnchorData.OnSavedCallback?.Invoke(_lastSaveAnchorData.Success);
                _lastSaveAnchorData = null;
            }

            if (_saveAnchorPendingQueue.Count > 0 && Interlocked.Exchange(ref _saveAnchorUsingResource, 1) == 0)
            {
                var saveAnchorData = _saveAnchorPendingQueue.Dequeue();
                Interlocked.Exchange(ref saveAnchorData.UsingResource, 1);
                Thread thread = new Thread(SaveAnchorAsync);
                thread.Start(saveAnchorData);
                _lastSaveAnchorData = saveAnchorData;
            }

            if (_lastLoadAnchorData != null && Interlocked.Exchange(ref _lastLoadAnchorData.UsingResource, 1) == 0)
            {
                if (_lastLoadAnchorData.Success)
                {
                    GameObject go = new GameObject
                    {
                        name = _lastLoadAnchorData.AnchorName,
                        transform =
                        {
                            position = Vector3.zero,
                            rotation = Quaternion.identity
                        }
                    };
                    go.SetActive(false);
                    var provider = (SpatialAnchorsSubsystem.SpatialAnchorsProvider)_subsystem.GetProvider();
                    provider.SetPersistentAnchorCandidate(new SpatialAnchor(_lastLoadAnchorData.AnchorHandle, _lastLoadAnchorData.SpaceHandle, Pose.identity, _lastLoadAnchorData.AnchorName));
                    go.AddComponent<ARAnchor>();
                    go.SetActive(true);
                }

                _lastLoadAnchorData.OnLoadedCallback?.Invoke(_lastLoadAnchorData.Success);
                _lastLoadAnchorData = null;
            }

            if (_loadAnchorPendingQueue.Count > 0 && Interlocked.Exchange(ref _loadAnchorUsingResource, 1) == 0)
            {
                var loadAnchorData = _loadAnchorPendingQueue.Dequeue();
                Interlocked.Exchange(ref loadAnchorData.UsingResource, 1);
                Thread thread = new Thread(LoadSavedAnchorAsync);
                thread.Start(loadAnchorData);
                _lastLoadAnchorData = loadAnchorData;
            }
        }

        private void LoadStore()
        {
            if (_feature.TryCreateSpatialAnchorStoreConnection(out _spatialAnchorStore))
            {
                _isStoreLoaded = true;
            }
        }

        private void UnloadStore()
        {
            if (_isStoreLoaded)
            {
                _feature.TryDestroySpatialAnchorStoreConnection(_spatialAnchorStore);
            }
        }

        private void SaveAnchorAsync(object saveAnchorDataObject)
        {
            var data = (SaveAnchorData)saveAnchorDataObject;
            if (_feature.TryPersistSpatialAnchor(_spatialAnchorStore, data.Anchor.trackableId.subId1, data.AnchorName))
            {
                data.Success = true;
            }

            // The resource has finished being used.
            Interlocked.Exchange(ref data.UsingResource, 0);
            Interlocked.Exchange(ref _saveAnchorUsingResource, 0);
        }
    }
}
