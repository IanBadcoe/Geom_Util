// #define PROFILE_ON

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using Geom_Util.Interfaces;

using Godot_Util;

using Geom_Util.Immutable;

using System;

namespace Geom_Util
{
    public interface ISpatialValue<KeyT> : IBounded
    {
        KeyT Key { get; set; }
    }

    public enum SpatialStatus
    {
        Enabled,
        Disabled
    }
    // store a value against an IBounded key, so we can search by the key value, OR, by location in space
    //
    // Technicality, the spatial component of this is optional, construct with "spatial_status_part" false
    // to turn it off, this is a performance optimisation as some of the things we store don't always need the
    // expensive SpaceMap (but when they need it, they really need it,  for other perfomance reasons)
    // it can be turned on (expensive) and off (cheap) as required
    [DebuggerDisplay("{Forwards.Count} ({SpaceMap.GetBounds().Min.X}, {SpaceMap.GetBounds().Min.Y}, {SpaceMap.GetBounds().Min.Z}) -> ({SpaceMap.GetBounds().Max.X}, {SpaceMap.GetBounds().Max.Y}, {SpaceMap.GetBounds().Max.Z})")]
    public class SpatialDictionary<Key, ValueT>
         : IEnumerable<KeyValuePair<Key, ValueT>> where ValueT : class, ISpatialValue<Key>
    {
        Dictionary<Key, ValueT> ForwardsInner = [];

        RTree<ValueT> SpaceMap;

        public SpatialDictionary(SpatialStatus spatial_status = SpatialStatus.Disabled)
        {
            if (spatial_status == SpatialStatus.Enabled)
            {
                SpaceMap = [];
            }
        }

        public SpatialDictionary(IEnumerable<KeyValuePair<Key, ValueT>> pairs, SpatialStatus spatial_status = SpatialStatus.Disabled)
            : this(spatial_status)
        {
            foreach(var pair in pairs)
            {
                this[pair.Key] = pair.Value;
            }
        }

        public SpatialStatus SpatialStatus
        {
            get => SpaceMap != null ? SpatialStatus.Enabled : SpatialStatus.Disabled;
            set
            {
                if (value != SpatialStatus)
                {
                    if (value == SpatialStatus.Enabled)
                    {
                        SpaceMap = [];

                        foreach (ValueT v in ForwardsInner.Values)
                        {
                            SpaceMap.Insert(v);
                        }
                    }
                    else
                    {
                        SpaceMap = null;
                    }
                }
            }
        }

        public IEnumerable<Key> FindKeys(ImBounds bounds, IReadOnlyRTree.SearchMode mode)
        {
            if (SpaceMap == null)
            {
                throw new InvalidOperationException();
            }

            foreach (ValueT value in SpaceMap.Search(bounds, mode))
            {
                yield return value.Key;
            }
        }

        public IEnumerable<ValueT> FindValues(ImBounds bounds, IReadOnlyRTree.SearchMode mode)
        {
            if (SpaceMap == null)
            {
                throw new InvalidOperationException();
            }

            foreach(ValueT idx in SpaceMap.Search(bounds, mode)) {
                yield return idx;
            }
        }

        public ValueT this [Key idx]
        {
            get => ForwardsInner[idx];
            set => Add(value, idx);
        }

        public int Count
        {
            get => ForwardsInner.Count;
        }

        void Add(ValueT t1, Key t2)
        {
            ForwardsInner[t2] = t1;

            if (SpaceMap != null)
            {
                SpaceMap.Insert(t1);
            }

            t1.Key = t2;
        }

        public void Remove(ValueT value)
        {
            Key idx = value.Key;

            PoorMansProfiler.Start("Forwards");
            ForwardsInner.Remove(idx);
            PoorMansProfiler.End("Forwards");

            if (SpaceMap != null)
            {
                PoorMansProfiler.Start("Spatial");
                SpaceMap.Remove(value);
                PoorMansProfiler.End("Spatial");
            }
        }

        public bool Contains(ValueT val)
        {
            return Contains(val.Key);
        }

        public bool Contains(Key idx)
        {
            return ForwardsInner.ContainsKey(idx);
        }

        public IEnumerable<Key> Keys
        {
            get => ForwardsInner.Keys;
        }

        public IEnumerable<ValueT> Values
        {
            get => ForwardsInner.Values;
        }

        public IEnumerator<KeyValuePair<Key, ValueT>> GetEnumerator() => ForwardsInner.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ForwardsInner.GetEnumerator();

        public IReadOnlyDictionary<Key, ValueT> Forwards { get => ForwardsInner; }
    }
}