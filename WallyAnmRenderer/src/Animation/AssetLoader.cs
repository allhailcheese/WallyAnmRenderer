using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using BrawlhallaAnimLib.Gfx;
using BrawlhallaAnimLib.Math;
using WallyAnmSpinzor;

namespace WallyAnmRenderer;

public sealed class AssetLoader(string brawlPath)
{
    private string _brawlPath = brawlPath;
    public string BrawlPath
    {
        get => _brawlPath;
        set
        {
            if (_brawlPath == value) return;

            _brawlPath = value;
            _swfFileOverrides.BrawlPath = value;
            ClearSwfFileCache();
            ClearAnmCache();
            ClearSwfShapeCache();
        }
    }

    public AnmVersionEnum AnmVersion
    {
        get => _anmFileCache.AnmVersion;
        set => _anmFileCache.AnmVersion = value;
    }

    private readonly AnmFileCache _anmFileCache = new();
    private readonly TextureCache _textureCache = new();
    private readonly SwfFileCache _swfFileCache = new();
    private readonly SwfShapeCache _swfShapeCache = new();
    private readonly SwfOverrideMap _swfFileOverrides = new(brawlPath);

    public bool IsAnmLoading(string filePath)
    {
        string finalPath = Path.GetFullPath(Path.Combine(_brawlPath, filePath));
        return _anmFileCache.IsLoading(finalPath);
    }

    public bool IsAnmErrored(string filePath)
    {
        string finalPath = Path.GetFullPath(Path.Combine(_brawlPath, filePath));
        return _anmFileCache.IsErrored(finalPath);
    }

    public bool TryGetAnm(string filePath, [MaybeNullWhen(false)] out AnmFile anm)
    {
        string finalPath = Path.GetFullPath(Path.Combine(_brawlPath, filePath));
        return _anmFileCache.TryGetCached(finalPath, out anm);
    }

    public void RemoveCachedAnm(string filePath)
    {
        string finalPath = Path.GetFullPath(Path.Combine(_brawlPath, filePath));
        _anmFileCache.RemoveCached(finalPath);
    }

    public AnmFile? LoadAnm(string filePath)
    {
        string finalPath = Path.GetFullPath(Path.Combine(_brawlPath, filePath));
        ValueTask<AnmFile> task = _anmFileCache.LoadAsync(finalPath);
        if (task.IsCompletedSuccessfully)
            return task.Result;
        return null;
    }

    public bool TryGetAnmClass(string name, [MaybeNullWhen(false)] out AnmClass @class)
    {
        return _anmFileCache.TryGetAnmClass(name, out @class);
    }

    public bool IsSwfLoading(string filePath)
    {
        string finalPath = Path.GetFullPath(Path.Combine(_brawlPath, filePath));
        return _swfFileCache.IsLoading(finalPath);
    }

    public bool IsSwfErrored(string filePath)
    {
        string finalPath = Path.GetFullPath(Path.Combine(_brawlPath, filePath));
        return _swfFileCache.IsErrored(finalPath);
    }

    public bool TryGetSwf(string filePath, [MaybeNullWhen(false)] out SwfFileData swf)
    {
        string finalPath = Path.GetFullPath(Path.Combine(_brawlPath, filePath));
        return _swfFileCache.TryGetCached(finalPath, out swf);
    }

    public void RemoveCachedSwf(string filePath)
    {
        string finalPath = Path.GetFullPath(Path.Combine(_brawlPath, filePath));
        _swfFileCache.RemoveCached(finalPath);
    }

    public ValueTask<SwfFileData> LoadSwf(string filePath)
    {
        string finalPath = Path.GetFullPath(Path.Combine(_brawlPath, filePath));
        if (_swfFileOverrides.TryGetValue(finalPath, out SwfOverride overrideData))
            return ValueTask.FromResult(overrideData.Data);
        return _swfFileCache.LoadAsync(finalPath);
    }

    public Texture2DWrapper? LoadTexture(ISpriteData spriteData)
    {
        string file = Path.GetFullPath(Path.Combine(_brawlPath, spriteData.File));
        return _textureCache.GetCachedOrLoad(file, spriteData.XOffset, spriteData.YOffset).ToNullable();
    }

    public Texture2DWrapper? LoadShapeFromSwf(string filePath, string spriteName, ushort shapeId, double animScale, Dictionary<uint, uint> colorSwapDict, ColorTransform colorTransform)
    {
        ValueTask<SwfFileData> task = LoadSwf(filePath);
        if (!task.IsCompleted)
            return null;
        else if (!task.IsCompletedSuccessfully)
            return new();
        SwfFileData swf = task.Result;

        return _swfShapeCache.GetCachedOrLoad(swf, spriteName, shapeId, animScale, colorSwapDict, colorTransform).ToNullable();
    }

    public const int MAX_TEXTURE_UPLOADS_PER_FRAME = 5;
    public const int MAX_SWF_TEXTURE_UPLOADS_PER_FRAME = 5;
    public void Upload()
    {
        _textureCache.Upload(MAX_TEXTURE_UPLOADS_PER_FRAME);
        _swfShapeCache.Upload(MAX_SWF_TEXTURE_UPLOADS_PER_FRAME);
    }

    public void AddSwfOverride(string relativePath, SwfOverride data)
    {
        string fullPath = Path.GetFullPath(Path.Combine(_brawlPath, relativePath));
        _swfFileOverrides[fullPath] = data;
    }

    public bool RemoveSwfOverride(string relativePath)
    {
        string fullPath = Path.GetFullPath(Path.Combine(_brawlPath, relativePath));
        return _swfFileOverrides.Remove(fullPath);
    }

    public Task ReloadOverride(string relativePath)
    {
        string fullPath = Path.GetFullPath(Path.Combine(_brawlPath, relativePath));
        return _swfFileOverrides.ReloadOverride(fullPath);
    }

    public IEnumerable<SwfOverride> Overrides => _swfFileOverrides.Overrides;

    public void ClearTextureCache()
    {
        _textureCache.Clear();
    }

    public void ClearSwfShapeCache()
    {
        _swfShapeCache.Clear();
    }

    public void ClearSwfFileCache()
    {
        _swfFileCache.Clear();
    }

    public void ClearAnmCache()
    {
        _anmFileCache.Clear();
    }
}