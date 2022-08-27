using MoeTag.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoeTag.Graphics
{
    internal class MoeContentManager : IDisposable
    {
        // Load On Buffer
        private readonly Stack<MoeContentModel> _modelBuffer; // ASYNC FROM THREADING VV
        private readonly List<MoeContentModel> _models; // SYNC TO RENDER (BROWSE) VV
        private readonly List<MoeContentModel> _previewModels; // SYNC TO RENDER (PREVIEW) VV

        private MoeContentModel _previewModel;

        public MoeContentManager()
        {
            _modelBuffer = new Stack<MoeContentModel>();
            _models = new List<MoeContentModel>();
            _previewModels = new List<MoeContentModel>();
            _previewModel = MoeContentModel.EmptyModel;
        }

        public bool UpdateDataBuffer()
        {
            while (_modelBuffer.TryPop(out MoeContentModel? _dataBufferData))
            {
                if (_dataBufferData != null && _models != null)
                {
                    _models.Add(_dataBufferData);
                }
            }
            if (_models != null)
            {
                foreach (MoeContentModel model in _models)
                {
                    if (model != null)
                    {
                        model.CheckForContentGeneration();
                    }
                }
            }

            return _modelBuffer.Count == 0;
        }

        public void DisposeUnusedContent(bool clearInUse = false)
        {
            foreach (var model in _modelBuffer)
            {
                if (IsOpenPreview(model) && !clearInUse) { model.DisposeThumbnail(); continue; }
                model.Dispose();
            }
            foreach (var model in _models)
            {
                if (IsOpenPreview(model) && !clearInUse) { model.DisposeThumbnail(); continue; }
                model.Dispose();
            }

            _models.Clear();
            _modelBuffer.Clear();

            if (clearInUse)
            {
                _previewModel = MoeContentModel.EmptyModel;
            }

            GC.Collect();
        }

        public bool IsCurrentPreview(MoeContentModel model)
        {
            if (IsOpenPreview(model))
            {
                return _previewModel == model;
            }
            return false;
        }

        public bool IsOpenPreview(MoeContentModel model)
        {
            return _previewModels.Contains(model);
        }

        public IReadOnlyCollection<MoeContentModel> GetModels()
        {
            return _models;
        }

        public bool IsEmpty()
        {
            if (_modelBuffer == null) return true;
            if (_previewModels == null) return true;
            return _modelBuffer.Count == 0 && _previewModels.Count == 0;
        }

        public IReadOnlyCollection<MoeContentModel> GetModelPreviews()
        {
            return _previewModels;
        }

        public MoeContentModel GetCurrentModelPreview()
        {
            return _previewModel;
        }

        public void SetPreview(MoeContentModel model)
        {
            _previewModel = model;
        }

        public MoeContentModel AddModel(string thumbnailUrl, string previewUrl)
        {
            var model = new MoeContentModel(thumbnailUrl, previewUrl);
            _modelBuffer.Push(model);
            return model;
        }

        public bool AddPreview(MoeContentModel model)
        {
            if (_models.Contains(model))
            {
                _previewModels.Add(model);
                return true;
            }
            return false;
        }

        public void ShiftDownPreview()
        {
            if (_previewModels.Count > 0)
            {
                _previewModel = _previewModels[_previewModels.Count - 1];
            }
            else
            {
                _previewModel = MoeContentModel.EmptyModel;
            }
        }

        public bool RemovePreview(MoeContentModel model)
        {
            bool arrayRemoved = false;
            if (_previewModels.Contains(model))
            {
                _previewModels.Remove(model);
                arrayRemoved = true;
            }
            _previewModel.DisposePreview();

            GC.Collect();

            return arrayRemoved;
        }

        public void Dispose()
        {
            DisposeUnusedContent(true);
            MoeContentModel.EmptyModel.Dispose();
        }
    }
}
