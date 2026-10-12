using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ImGuiNET;
using SwfLib.Tags;
using WallyAnmSpinzor;

namespace WallyAnmRenderer;

public sealed class AnmWindow
{
    private static readonly AnmVersionEnum[] ANM_FORMAT_OPTIONS = [
        AnmVersionEnum.From904ToCurrent,
        AnmVersionEnum.From710To904,
        AnmVersionEnum.To704,
    ];

    private static readonly string[] ANM_FORMAT_LABELS = [
        "9.05-Current",
        "7.10-9.04",
        "???-7.04",
    ];

    private bool _open = true;
    public bool Open { get => _open; set => _open = value; }

    public event EventHandler<string>? FileUnloaded;

    private string _anmFileFilter = "";
    private readonly Dictionary<string, string> _animationFilterState = [];

    private string _swfFileFilter = "";
    private readonly Dictionary<string, string> _swfFilterState = [];

    public void Show(string? brawlPath, AssetLoader? assetLoader, GfxInfo info)
    {
        ImGui.Begin("Animations", ref _open);

        if (brawlPath is null)
        {
            ImGui.TextWrapped("You must select your brawlhalla path to choose an animation");
            ImGui.End();
            return;
        }

        if (assetLoader is null)
        {
            ImGui.End();
            return;
        }

        if (ImGui.BeginTabBar("options", ImGuiTabBarFlags.NoCloseWithMiddleMouseButton))
        {
            if (ImGui.BeginTabItem("ANM Animations"))
            {
                AnmTab(brawlPath, assetLoader, info);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("SWF Sprites"))
            {
                SwfTab(brawlPath, assetLoader, info);
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        ImGui.End();
    }

    private void AnmTab(string brawlPath, AssetLoader assetLoader, GfxInfo info)
    {
        int index = Array.FindIndex(ANM_FORMAT_OPTIONS, v => v == assetLoader.AnmVersion);
        if (ImGui.Combo("Anm format", ref index, ANM_FORMAT_LABELS, ANM_FORMAT_LABELS.Length))
            assetLoader.AnmVersion = ANM_FORMAT_OPTIONS[index];

        if (assetLoader.AnmVersion != AnmVersionEnum.From904ToCurrent)
        {
            ImGui.PushTextWrapPos();
            ImGui.TextColored(Colors.WARN_TEXT, "You've selected an older anm format. Unless you are working with older patches, loading anm files will fail.");
            ImGui.PopTextWrapPos();
        }

        string brawlAnimPath = Path.Join(brawlPath, "anims");
        string[] files = Directory.Exists(brawlAnimPath) ? Directory.GetFiles(brawlAnimPath, "*.anm") : [];

        ImGui.InputText("Filter files", ref _anmFileFilter, 256);
        foreach (string absolutePath in files)
        {
            string fileName = Path.GetRelativePath(brawlAnimPath, absolutePath);
            if (!fileName.Contains(_anmFileFilter, StringComparison.CurrentCultureIgnoreCase))
                continue;
            string relativePath = Path.GetRelativePath(brawlPath, absolutePath);

            ImGui.PushID(fileName);
            if (assetLoader.IsAnmLoading(relativePath))
            {
                ImGui.Text(fileName);
                ImGui.SameLine();
                if (ImGui.Button("Cancel"))
                {
                    assetLoader.RemoveCachedAnm(absolutePath);
                }
                ImGui.SameLine();
                ImGui.TextDisabled("Loading...");
            }
            else if (assetLoader.IsAnmErrored(relativePath))
            {
                ImGui.Text(fileName);
                ImGui.SameLine();
                if (ImGui.Button("Retry"))
                {
                    assetLoader.RemoveCachedAnm(absolutePath);
                    assetLoader.LoadAnm(relativePath);
                }
                ImGui.SameLine();
                ImGui.TextColored(Colors.ERROR_TEXT, "Error!");
            }
            else if (assetLoader.TryGetAnm(relativePath, out AnmFile? anm))
            {
                void unloadButton()
                {
                    ImGui.SameLine();
                    if (ImGui.Button("Unload"))
                    {
                        FileUnloaded?.Invoke(this, relativePath);
                        assetLoader.RemoveCachedAnm(absolutePath);
                    }
                }

                ImGui.SetNextItemAllowOverlap();
                if (ImGui.TreeNode(fileName))
                {
                    unloadButton();

                    foreach ((string anmClassName, AnmClass anmClass) in anm.Classes)
                    {
                        string[] parts = anmClassName.Split('/', 2);
                        string animFile = parts[0];
                        string animClass = parts[1];

                        if (ImGui.TreeNode(animClass))
                        {
                            string filter = _animationFilterState.GetValueOrDefault(anmClassName, "");
                            if (ImGui.InputText("Filter animations", ref filter, 256))
                            {
                                _animationFilterState[anmClassName] = filter;
                            }

                            if (ImGui.BeginListBox(""))
                            {
                                foreach (string animation in anmClass.Animations.Keys)
                                {
                                    if (!animation.Contains(filter, StringComparison.CurrentCultureIgnoreCase))
                                        continue;

                                    bool selected =
                                        animFile == info.AnimFile &&
                                        animClass == info.AnimClass &&
                                        animation == info.Animation;

                                    if (selected) ImGui.PushStyleColor(ImGuiCol.Text, Colors.SELECTED_OPTION);
                                    if (ImGui.Selectable(animation, selected))
                                    {
                                        info.SourceFilePath = relativePath;
                                        info.AnimFile = animFile;
                                        info.AnimClass = animClass;
                                        info.Animation = animation;
                                    }
                                    if (selected) ImGui.PopStyleColor();
                                }
                                ImGui.EndListBox();
                            }

                            ImGui.TreePop();
                        }
                    }

                    ImGui.TreePop();
                }
                else
                {
                    unloadButton();
                }
            }
            else
            {
                ImGui.Text(fileName);
                ImGui.SameLine();
                if (ImGui.Button("Load"))
                {
                    assetLoader.LoadAnm(relativePath);
                }
            }
            ImGui.PopID();
        }
    }

    private void SwfTab(string brawlPath, AssetLoader assetLoader, GfxInfo info)
    {
        ImGui.PushTextWrapPos();
        ImGui.TextColored(Colors.NOTE_TEXT, "In this tab you can view individual sprites. To view a full animation, go to the ANM Animations tab");
        ImGui.PopTextWrapPos();

        string[] baseFiles = Directory.Exists(brawlPath) ? Directory.GetFiles(brawlPath, "*.swf") : [];
        string bonesSwfPath = Path.Join(brawlPath, "bones");
        string[] bonesFiles = Directory.Exists(bonesSwfPath) ? Directory.GetFiles(bonesSwfPath, "*.swf") : [];

        ImGui.InputText("Filter files", ref _swfFileFilter, 256);
        foreach (string absolutePath in baseFiles.Concat(bonesFiles))
        {
            string animFile = Path.GetRelativePath(brawlPath, absolutePath);
            string displayName = Path.GetFileName(animFile);
            if (!displayName.Contains(_swfFileFilter, StringComparison.CurrentCultureIgnoreCase))
                continue;
            string relativePath = Path.GetRelativePath(brawlPath, absolutePath);

            ImGui.PushID(animFile);
            if (assetLoader.IsSwfLoading(relativePath))
            {
                ImGui.Text(displayName);
                ImGui.SameLine();
                if (ImGui.Button("Cancel"))
                {
                    assetLoader.RemoveCachedSwf(absolutePath);
                }
                ImGui.SameLine();
                ImGui.TextDisabled("Loading...");
            }
            else if (assetLoader.IsSwfErrored(relativePath))
            {
                ImGui.Text(displayName);
                ImGui.SameLine();
                if (ImGui.Button("Retry"))
                {
                    assetLoader.RemoveCachedSwf(absolutePath);
                    assetLoader.LoadSwf(absolutePath).AsTask();
                }
                ImGui.SameLine();
                ImGui.TextColored(Colors.ERROR_TEXT, "Error!");
            }
            else if (assetLoader.TryGetSwf(relativePath, out SwfFileData? swf))
            {
                void unloadButton()
                {
                    ImGui.SameLine();
                    if (ImGui.Button("Unload"))
                    {
                        FileUnloaded?.Invoke(this, relativePath);
                        assetLoader.RemoveCachedSwf(absolutePath);
                    }
                }

                ImGui.SetNextItemAllowOverlap();
                if (ImGui.TreeNode(displayName))
                {
                    unloadButton();

                    string filter = _swfFilterState.GetValueOrDefault(animFile, "");
                    if (ImGui.InputText("Filter sprites", ref filter, 256))
                    {
                        _swfFilterState[animFile] = filter;
                    }

                    if (ImGui.BeginListBox(""))
                    {
                        foreach ((_, DefineSpriteTag spriteTag) in swf.SpriteTags)
                        {
                            string spriteName;
                            string animClass;
                            if (swf.ReverseSymbolClass.TryGetValue(spriteTag.SpriteID, out string? symbolName))
                            {
                                animClass = symbolName;
                                spriteName = spriteTag.SpriteID + " " + symbolName;
                            }
                            else
                            {
                                animClass = Loader.SPRITE_ID_PREFIX + spriteTag.SpriteID;
                                spriteName = spriteTag.SpriteID.ToString();
                            }

                            if (!spriteName.Contains(filter, StringComparison.CurrentCultureIgnoreCase))
                                continue;

                            bool selected =
                                animFile == info.AnimFile &&
                                animClass == info.AnimClass &&
                                string.IsNullOrEmpty(info.Animation);

                            if (selected) ImGui.PushStyleColor(ImGuiCol.Text, Colors.SELECTED_OPTION);
                            if (ImGui.Selectable(spriteName, selected))
                            {
                                info.SourceFilePath = relativePath;
                                info.AnimFile = animFile;
                                info.AnimClass = animClass;
                                info.Animation = "";
                            }
                            if (selected) ImGui.PopStyleColor();
                        }
                        ImGui.EndListBox();
                    }

                    ImGui.TreePop();
                }
                else
                {
                    unloadButton();
                }
            }
            else
            {
                ImGui.Text(displayName);
                ImGui.SameLine();
                if (ImGui.Button("Load"))
                {
                    assetLoader.LoadSwf(relativePath).AsTask();
                }
            }
            ImGui.PopID();
        }
    }
}