using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using SwfLib.Tags;
using SwfLib.Tags.ShapeTags;
using SwfLib.Tags.TextTags;
using SwfLib.Tags.ShapeMorphingTags;
using BrawlhallaAnimLib;
using BrawlhallaAnimLib.Anm;
using BrawlhallaAnimLib.Gfx;
using BrawlhallaLangReader;
using WallyAnmSpinzor;

namespace WallyAnmRenderer;

public sealed class Loader : ILoader
{
    // hack to be able to store a sprite id in the anim class
    public const string SPRITE_ID_PREFIX = "\uffff";

    private CancellationTokenSource? _loadSwzToken = null;
    private CancellationTokenSource? _loadLangToken = null;
    private Task<BoneDatabase>? _boneDatabase = null;

    public string BrawlPath
    {
        get => AssetLoader.BrawlPath;
        set => AssetLoader.BrawlPath = value;
    }

    public uint Key { get; set; }

    public AssetLoader AssetLoader { get; }
    public SwzFiles? SwzFiles { get; private set; } = null;
    public LangFile? LangFile { get; private set; } = null;

    public Loader(string brawlPath, uint key)
    {
        AssetLoader = new(brawlPath);
        Key = key;
    }

    public async Task LoadFilesAsync(CancellationToken cancellationToken = default)
    {
        await LoadSwzAsync(cancellationToken);
        try
        {
            await LoadLangAsync(cancellationToken);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            // lang loading failed, we might be in an older version. try load from swz.
            void merge(LangFile? lang)
            {
                if (lang is null || lang.Entries.Count == 0)
                    return;
                LangFile ??= new() { Header = 0, Entries = [] };
                foreach ((string key, string text) in lang.Entries)
                    LangFile.Entries[key] = text;
            }
            merge(SwzFiles?.Engine?.StringTable);
            merge(SwzFiles?.Init?.LoreStringTable);

            // was not present in swz, rethrow
            if (LangFile is null)
                throw;
        }
    }

    public async Task LoadSwzAsync(CancellationToken cancellationToken = default)
    {
        CancelLoadSwz();
        CancellationTokenSource source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _loadSwzToken = source;
        SwzFiles = await SwzFiles.NewAsync(BrawlPath, Key, source.Token);
    }

    public async Task LoadLangAsync(CancellationToken cancellationToken = default)
    {
        CancelLoadLang();
        CancellationTokenSource source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _loadLangToken = source;
        LangFile = await LangFile.LoadAsync(Path.Join(BrawlPath, "languages", "language.1.bin"), source.Token);
    }

    private void CancelLoadSwz()
    {
        if (_loadSwzToken is null) return;
        _loadSwzToken.Cancel();
        _loadSwzToken = null;
    }

    private void CancelLoadLang()
    {
        if (_loadLangToken is null) return;
        _loadLangToken.Cancel();
        _loadLangToken = null;
    }

    public bool SwfExists(string swfPath)
    {
        return File.Exists(Path.Join(BrawlPath, swfPath));
    }

    public ValueTask<IAnmClass?> GetAnmClass(string classIdentifier)
    {
        AnmClassAdapter? impl()
        {
            if (AssetLoader.TryGetAnmClass(classIdentifier, out AnmClass? @class))
                return new(@class);
            return null;
        }

        return ValueTask.FromResult<IAnmClass?>(impl());
    }

    public ValueTask<ISpriteData?> GetSpriteData(string boneName, string setName)
    {
        ISpriteData? impl()
        {
            if (SwzFiles?.Game is null)
                return null;

            SpriteData spriteData = SwzFiles.Game.SpriteData;
            if (spriteData.TryGetSpriteData(setName, boneName, out SpriteDataInfo? info))
                return info;
            return null;
        }

        return ValueTask.FromResult(impl());
    }

    public ValueTask<string?> GetBoneName(uint boneId)
    {
        string? impl()
        {
            if (SwzFiles?.Init is null)
                return null;

            BoneTypes boneTypes = SwzFiles.Init.BoneTypes;
            if (boneId == 0 || boneId > boneTypes.BoneCount)
                return null;

            return boneTypes[(int)(boneId - 1)];
        }

        return ValueTask.FromResult(impl());
    }

    public ValueTask<string?> GetBoneFilePath(string boneName)
    {
        string? impl()
        {
            if (SwzFiles?.Init is null)
                return null;

            BoneSources boneSources = SwzFiles.Init.BoneSources;
            if (boneSources.TryGetBoneFilePath(boneName, out string? bonePath))
                return bonePath;
            return null;
        }

        return ValueTask.FromResult(impl());
    }

    public async ValueTask<uint[]?> GetScriptAVar(string swfPath, string spriteName)
    {
        SwfFileData swf = await AssetLoader.LoadSwf(swfPath);
        return swf.SpriteA.GetValueOrDefault(spriteName);
    }

    public async ValueTask<ushort?> GetSymbolId(string swfPath, string symbolName)
    {
        if (symbolName.StartsWith(SPRITE_ID_PREFIX))
            return ushort.TryParse(symbolName[SPRITE_ID_PREFIX.Length..], out ushort result) ? result : null;

        SwfFileData swf = await AssetLoader.LoadSwf(swfPath);
        if (swf.SymbolClass.TryGetValue(symbolName, out ushort symbolId))
            return symbolId;
        return null;
    }

    public async ValueTask<SwfTagBase?> GetTag(string swfPath, ushort tagId)
    {
        SwfFileData swf = await AssetLoader.LoadSwf(swfPath);
        if (swf.SpriteTags.TryGetValue(tagId, out DefineSpriteTag? sprite))
            return sprite;
        if (swf.ShapeTags.TryGetValue(tagId, out ShapeBaseTag? shape))
            return shape;
        if (swf.MorphShapeTags.TryGetValue(tagId, out ShapeMorphingBaseTag? morphShape))
            return morphShape;
        if (swf.TextTags.TryGetValue(tagId, out DefineTextBaseTag? text))
            return text;
        if (swf.EditTextTags.TryGetValue(tagId, out DefineEditTextTag? editText))
            return editText;
        return null;
    }

    public bool TryGetStringName(string stringKey, [MaybeNullWhen(false)] out string stringName)
    {
        if (LangFile is null)
        {
            stringName = null;
            return false;
        }

        return LangFile.Entries.TryGetValue(stringKey, out stringName);
    }

    public async ValueTask<IBoneDatabase> GetBoneDatabase()
    {
        return await (_boneDatabase ??= BoneDatabase.NewAsync(BrawlPath).AsTask());
    }
}