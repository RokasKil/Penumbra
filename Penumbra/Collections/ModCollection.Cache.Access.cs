using OtterGui.Classes;
using Penumbra.GameData.Enums;
using Penumbra.Mods;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Penumbra.Interop.Structs;
using Penumbra.Meta.Files;
using Penumbra.Meta.Manipulations;
using Penumbra.String.Classes;
using CharacterUtility = Penumbra.Interop.Services.CharacterUtility;
using Penumbra.Collections.Cache;

namespace Penumbra.Collections;

public partial class ModCollection
{
    // Only active collections need to have a cache.
    internal ModCollectionCache? _cache;

    public bool HasCache
        => _cache != null;


    // Handle temporary mods for this collection.
    public void Apply(TemporaryMod tempMod, bool created)
    {
        if (created)
            _cache?.AddMod(tempMod, tempMod.TotalManipulations > 0);
        else
            _cache?.ReloadMod(tempMod, tempMod.TotalManipulations > 0);
    }

    public void Remove(TemporaryMod tempMod)
    {
        _cache?.RemoveMod(tempMod, tempMod.TotalManipulations > 0);
    }

    public IEnumerable<Utf8GamePath> ReverseResolvePath(FullPath path)
        => _cache?.ReverseResolvePath(path) ?? Array.Empty<Utf8GamePath>();

    public HashSet<Utf8GamePath>[] ReverseResolvePaths(string[] paths)
        => _cache?.ReverseResolvePaths(paths) ?? paths.Select(_ => new HashSet<Utf8GamePath>()).ToArray();

    public FullPath? ResolvePath(Utf8GamePath path)
        => _cache?.ResolvePath(path);

    // Force a file to be resolved to a specific path regardless of conflicts.
    internal void ForceFile(Utf8GamePath path, FullPath fullPath)
    {
        if (CheckFullPath(path, fullPath))
            _cache!.ResolvedFiles[path] = new ModPath(Mod.ForcedFiles, fullPath);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool CheckFullPath(Utf8GamePath path, FullPath fullPath)
    {
        if (fullPath.InternalName.Length < Utf8GamePath.MaxGamePathLength)
            return true;

        Penumbra.Log.Error($"The redirected path is too long to add the redirection\n\t{path}\n\t--> {fullPath}");
        return false;
    }

    // Force a file resolve to be removed.
    internal void RemoveFile(Utf8GamePath path)
        => _cache!.ResolvedFiles.Remove(path);

    // Obtain data from the cache.
    internal MetaCache? MetaCache
        => _cache?.MetaManipulations;

    public bool GetImcFile(Utf8GamePath path, [NotNullWhen(true)] out ImcFile? file)
    {
        if (_cache != null)
            return _cache.MetaManipulations.GetImcFile(path, out file);

        file = null;
        return false;
    }

    internal IReadOnlyDictionary<Utf8GamePath, ModPath> ResolvedFiles
        => _cache?.ResolvedFiles ?? new Dictionary<Utf8GamePath, ModPath>();

    internal IReadOnlyDictionary<string, (SingleArray<IMod>, object?)> ChangedItems
        => _cache?.ChangedItems ?? new Dictionary<string, (SingleArray<IMod>, object?)>();

    internal IEnumerable<SingleArray<ModConflicts>> AllConflicts
        => _cache?.AllConflicts ?? Array.Empty<SingleArray<ModConflicts>>();

    internal SingleArray<ModConflicts> Conflicts(Mod mod)
        => _cache?.Conflicts(mod) ?? new SingleArray<ModConflicts>();

    public void SetFiles()
    {
        if (_cache == null)
        {
            Penumbra.CharacterUtility.ResetAll();
        }
        else
        {
            _cache.MetaManipulations.SetFiles();
            Penumbra.Log.Debug($"Set CharacterUtility resources for collection {Name}.");
        }
    }

    public void SetMetaFile(MetaIndex idx)
    {
        if (_cache == null)
            Penumbra.CharacterUtility.ResetResource(idx);
        else
            _cache.MetaManipulations.SetFile(idx);
    }

    // Used for short periods of changed files.
    public CharacterUtility.MetaList.MetaReverter TemporarilySetEqdpFile(GenderRace genderRace, bool accessory)
        => _cache?.MetaManipulations.TemporarilySetEqdpFile(genderRace, accessory)
         ?? Penumbra.CharacterUtility.TemporarilyResetResource(Interop.Structs.CharacterUtilityData.EqdpIdx(genderRace, accessory));

    public CharacterUtility.MetaList.MetaReverter TemporarilySetEqpFile()
        => _cache?.MetaManipulations.TemporarilySetEqpFile()
         ?? Penumbra.CharacterUtility.TemporarilyResetResource(MetaIndex.Eqp);

    public CharacterUtility.MetaList.MetaReverter TemporarilySetGmpFile()
        => _cache?.MetaManipulations.TemporarilySetGmpFile()
         ?? Penumbra.CharacterUtility.TemporarilyResetResource(MetaIndex.Gmp);

    public CharacterUtility.MetaList.MetaReverter TemporarilySetCmpFile()
        => _cache?.MetaManipulations.TemporarilySetCmpFile()
         ?? Penumbra.CharacterUtility.TemporarilyResetResource(MetaIndex.HumanCmp);

    public CharacterUtility.MetaList.MetaReverter TemporarilySetEstFile(EstManipulation.EstType type)
        => _cache?.MetaManipulations.TemporarilySetEstFile(type)
         ?? Penumbra.CharacterUtility.TemporarilyResetResource((MetaIndex)type);
}
