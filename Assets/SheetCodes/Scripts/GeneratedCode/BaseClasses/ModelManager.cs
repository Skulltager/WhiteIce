using System;
using System.Collections.Generic;
using UnityEngine;

namespace SheetCodes
{
	//Generated code, do not edit!

	public static class ModelManager
	{
        private static Dictionary<DatasheetType, LoadRequest> loadRequests;

        static ModelManager()
        {
            loadRequests = new Dictionary<DatasheetType, LoadRequest>();
        }

        public static void InitializeAll()
        {
            DatasheetType[] values = Enum.GetValues(typeof(DatasheetType)) as DatasheetType[];
            foreach(DatasheetType value in values)
                Initialize(value);
        }
		
        public static void Unload(DatasheetType datasheetType)
        {
            switch (datasheetType)
            {
                case DatasheetType.ComputeShader:
                    {
                        if (computeShaderModel == null || computeShaderModel.Equals(null))
                        {
                            Log(string.Format("Sheet Codes: Trying to unload model {0}. Model is not loaded.", datasheetType));
                            break;
                        }
                        Resources.UnloadAsset(computeShaderModel);
                        computeShaderModel = null;
                        LoadRequest request;
                        if (loadRequests.TryGetValue(DatasheetType.ComputeShader, out request))
                        {
                            loadRequests.Remove(DatasheetType.ComputeShader);
                            request.resourceRequest.completed -= OnLoadCompleted_ComputeShaderModel;
							foreach (Action<bool> callback in request.callbacks)
								callback(false);
                        }
                        break;
                    }
                default:
                    break;
            }
        }

        public static void Initialize(DatasheetType datasheetType)
        {
            switch (datasheetType)
            {
                case DatasheetType.ComputeShader:
                    {
                        if (computeShaderModel != null && !computeShaderModel.Equals(null))
                        {
                            Log(string.Format("Sheet Codes: Trying to Initialize {0}. Model is already initialized.", datasheetType));
                            break;
                        }

                        computeShaderModel = Resources.Load<ComputeShaderModel>("ScriptableObjects/ComputeShader");
                        LoadRequest request;
                        if (loadRequests.TryGetValue(DatasheetType.ComputeShader, out request))
                        {
                            Log(string.Format("Sheet Codes: Trying to initialize {0} while also async loading. Async load has been canceled.", datasheetType));
                            loadRequests.Remove(DatasheetType.ComputeShader);
                            request.resourceRequest.completed -= OnLoadCompleted_ComputeShaderModel;
							foreach (Action<bool> callback in request.callbacks)
								callback(true);
                        }
                        break;
                    }
                default:
                    break;
            }
        }

        public static void InitializeAsync(DatasheetType datasheetType, Action<bool> callback)
        {
            switch (datasheetType)
            {
                case DatasheetType.ComputeShader:
                    {
                        if (computeShaderModel != null && !computeShaderModel.Equals(null))
                        {
                            Log(string.Format("Sheet Codes: Trying to InitializeAsync {0}. Model is already initialized.", datasheetType));
                            callback(true);
                            break;
                        }
                        if(loadRequests.ContainsKey(DatasheetType.ComputeShader))
                        {
                            loadRequests[DatasheetType.ComputeShader].callbacks.Add(callback);
                            break;
                        }
                        ResourceRequest request = Resources.LoadAsync<ComputeShaderModel>("ScriptableObjects/ComputeShader");
                        loadRequests.Add(DatasheetType.ComputeShader, new LoadRequest(request, callback));
                        request.completed += OnLoadCompleted_ComputeShaderModel;
                        break;
                    }
                default:
                    break;
            }
        }

        private static void OnLoadCompleted_ComputeShaderModel(AsyncOperation operation)
        {
            LoadRequest request = loadRequests[DatasheetType.ComputeShader];
            computeShaderModel = request.resourceRequest.asset as ComputeShaderModel;
            loadRequests.Remove(DatasheetType.ComputeShader);
            operation.completed -= OnLoadCompleted_ComputeShaderModel;
            foreach (Action<bool> callback in request.callbacks)
                callback(true);
        }

		private static ComputeShaderModel computeShaderModel = default;
		public static ComputeShaderModel ComputeShaderModel
        {
            get
            {
                if (computeShaderModel == null)
                    Initialize(DatasheetType.ComputeShader);

                return computeShaderModel;
            }
        }
		
        private static void Log(string message)
        {
            Debug.LogWarning(message);
        }
	}
	
    public struct LoadRequest
    {
        public readonly ResourceRequest resourceRequest;
        public readonly List<Action<bool>> callbacks;

        public LoadRequest(ResourceRequest resourceRequest, Action<bool> callback)
        {
            this.resourceRequest = resourceRequest;
            callbacks = new List<Action<bool>>() { callback };
        }
    }
}
