//
// --------------------------------------------------------------------------
//  Gurux Ltd
//
//
//
// Filename:        $HeadURL$
//
// Version:         $Revision$,
//                  $Date$
//                  $Author$
//
// Copyright (c) Gurux Ltd
//
//---------------------------------------------------------------------------
//
//  DESCRIPTION
//
// This file is a part of Gurux Device Framework.
//
// Gurux Device Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Device Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
//
// This code is licensed under the GNU General Public License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------

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
                if (GXInternal.IsGenericDataType(it.Value.Type))
                {
                    object? value = row[it.Key];
                    if (value is not null && value is not DBNull)
                    {
                        Type type = it.Value.Type;
                        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                        {
                            type = Nullable.GetUnderlyingType(type);
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

            if (!levels.Any())
            {
                return new List<T>();
            }

            var roots = new List<TreeNode>();

            var childrenByParent = new Dictionary<TreeNode, Dictionary<(Type Type, object Id), TreeNode>>();
            var rootNodes = new Dictionary<object, TreeNode>();
            foreach (object[] row in rows)
            {
                List<TreeNode> rowNodes = [];
                foreach (TreeLevel level in levels)
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
