// --------------------------------------------------------------------------
// Gurux Ltd
//
// Copyright (c) Gurux Ltd.
//
// This file is part of Gurux Device Framework.
//
// Commit: $Format:%H$
// Date:   $Format:%cI$
//
// This software is available under either of the following licenses:
//
// 1. GNU General Public License, version 2 only (GPL-2.0-only).
//    See the LICENSE file included with this distribution.
//
// 2. A commercial license granted by Gurux Ltd under a separate written
//    agreement. Use under this option requires a valid commercial license
//    and is governed by the terms of that agreement.
//
// If you have not obtained a commercial license from Gurux Ltd,
// use of this software is governed by GPL-2.0-only.
//
// Commercial licensing: https://www.gurux.fi/
//
// SPDX-License-Identifier: GPL-2.0-only OR LicenseRef-Gurux-Commercial
// --------------------------------------------------------------------------

using Gurux.Common.Internal;
using Gurux.Service.Orm.Settings;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Gurux.Service.Orm.Internal
{
    readonly record struct TreeLevel(
    Type type,
    int IdIndex,
    Dictionary<int, GXSerializedItem> indexes);

    static class TreeBuilder
    {

        private static TreeNode CreateNode(
            GXDBSettings settings,
            TreeLevel level,
            object[] row)
        {
            object? id = row[level.IdIndex];
            if (id is null || id is DBNull)
            {
                throw new InvalidOperationException(
                    "Tree node ID cannot be null.");
            }
            var node = new TreeNode
            {
                Id = id,
                Target = GXInternal.CreateClass(level.type)
            };
            foreach (var it in level.indexes)
            {
                if (it.Value.Relation == null && GXJsonCollection.IsCollection(it.Value.Type))
                {
                    var stored = row[it.Key];
                    it.Value.Set(node.Target, stored is null or DBNull ? null : settings.ChangeType(stored, it.Value.Type));
                    continue;
                }
                if (GXInternal.IsGenericDataType(it.Value.Type))
                {
                    object? value = row[it.Key];
                    if (value is not null && value is not DBNull)
                    {
                        Type type = it.Value.Type;
                        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                        {
                            type = Nullable.GetUnderlyingType(type) ?? type;
                        }
                        value = settings.ChangeType(value, type);
                    }
                    else
                    {
                        value = null;
                    }
                    if (value != null)
                    {
                        it.Value.Set(node.Target, value);
                    }
                }
                else
                {
                    node.Setters.Add(it.Value);
                }
            }
            return node;
        }

        public static List<T> BuildTree<T>(
            GXDBSettings settings,
            IEnumerable<object[]> rows,
            params IEnumerable<TreeLevel> levels)
        {
            ArgumentNullException.ThrowIfNull(rows);
            ArgumentNullException.ThrowIfNull(levels);

            // The result type determines the root, independently of SQL column order.
            TreeLevel[] orderedLevels = levels.OrderByDescending(level => level.type == typeof(T)).ToArray();
            if (orderedLevels.Length == 0)
            {
                return new List<T>();
            }

            var roots = new List<TreeNode>();

            var childrenByParent = new Dictionary<TreeNode, Dictionary<(Type Type, object Id), TreeNode>>();
            var rootNodes = new Dictionary<object, TreeNode>();
            foreach (object[] row in rows)
            {
                List<TreeNode> rowNodes = [];
                foreach (TreeLevel level in orderedLevels)
                {
                    object? id = row[level.IdIndex];
                    if (id is null || id is DBNull)
                    {
                        break;
                    }
                    TreeNode node;
                    if (rowNodes.Count == 0)
                    {
                        if (!rootNodes.TryGetValue(id, out node!))
                        {
                            node = CreateNode(settings, level, row);
                            rootNodes.Add(id, node);
                            roots.Add(node);
                        }
                    }
                    else
                    {
                        TreeNode parent = GetParent(rowNodes, level.type);
                        if (!childrenByParent.TryGetValue(
                                parent,
                                out Dictionary<(Type Type, object Id), TreeNode>? children))
                        {
                            children = new Dictionary<(Type Type, object Id), TreeNode>();
                            childrenByParent.Add(parent, children);
                        }

                        var key = (level.type, id);
                        if (!children.TryGetValue(key, out node!))
                        {
                            node = CreateNode(settings, level, row);
                            children.Add(key, node);
                            parent.Childrens.Add(node);
                        }
                    }
                    rowNodes.Add(node);
                }
            }
            UpdateChildren(roots);
            return roots.Select(s => (T)s.Target).ToList();
        }

        private static TreeNode GetParent(
            List<TreeNode> rowNodes,
            Type childType)
        {
            for (int pos = rowNodes.Count - 1; pos != -1; --pos)
            {
                TreeNode parent = rowNodes[pos];
                if (parent.Setters.Any(s =>
                    GXInternal.GetPropertyType(s.Type).IsAssignableFrom(childType)))
                {
                    return parent;
                }
            }
            return rowNodes.Last();
        }


        private static void UpdateChildren(
            List<TreeNode> nodes)
        {
            foreach (var level in nodes)
            {
                UpdateChildren(level.Childrens);
                if (level.Childrens.Any())
                {
                    foreach (GXSerializedItem setter in GetChildSetters(level))
                    {
                        Type setterType = GXInternal.GetPropertyType(setter.Type);
                        var children = level.Childrens
                            .Where(c => setterType.IsAssignableFrom(c.Target.GetType()))
                            .Select(c => c.Target);
                        if (!typeof(System.Collections.IEnumerable).IsAssignableFrom(setter.Type))
                        {
                            object? child = children.FirstOrDefault();
                            if (child != null)
                            {
                                setter.Set(level.Target, child);
                            }
                        }
                        else
                        {
                            if (children.Any())
                            {
                                setter.Set(level.Target, GXInternal.ConvertListIfNeeded(children, setter.Type));
                            }
                        }
                    }
                }
            }
        }

        private static IEnumerable<GXSerializedItem> GetChildSetters(
            TreeNode node)
        {
            List<GXSerializedItem> setters = [.. node.Setters];
            Dictionary<string, GXSerializedItem> properties =
                GXSqlBuilder.GetProperties(node.Target.GetType());
            foreach (GXSerializedItem setter in properties.Values)
            {
                Type setterType = GXInternal.GetPropertyType(setter.Type);
                if (!setters.Contains(setter) &&
                    node.Childrens.Any(c => setterType.IsAssignableFrom(c.Target.GetType())))
                {
                    setters.Add(setter);
                }
            }
            return setters;
        }

    }
}
