﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Labradoratory.Fetch.ChangeTracking
{
    /// <summary>
    /// TODO
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <seealso cref="ICollection{T}" />
    public class ChangeTrackingCollection<T> : ICollection<T>, ITracksChanges
    {
        /// <summary>
        /// Initializes a new, empty instance of the <see cref="ChangeTrackingCollection{T}"/> class.
        /// </summary>
        public ChangeTrackingCollection() 
            : this(new List<T>(0))
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeTrackingCollection{T}"/> class that 
        /// containes the provided item.
        /// </summary>
        /// <param name="items">The items to add to the collection.</param>
        public ChangeTrackingCollection(IEnumerable<T> items)
        {
            Items = items.Select(i => new ChangeContainerItem<T>(i, ChangeTarget.Collection, ChangeAction.None)).ToList();
            Removed = new List<ChangeContainerItem<T>>();
        }

        private List<ChangeContainerItem<T>> Items { get; }
        private List<ChangeContainerItem<T>> Removed { get; }

        /// <inheritdoc />
        public bool HasChanges => Items.Any(i => i.HasChanges) || Removed.Count > 0;

        /// <inheritdoc />
        public int Count => Items.Count;

        /// <inheritdoc />
        public bool IsReadOnly => false;
        
        /// <inheritdoc />
        public void Add(T item)
        {
            Items.Add(new ChangeContainerItem<T>(item, ChangeTarget.Collection, ChangeAction.Add));
        }

        /// <inheritdoc />
        public void Clear()
        {
            while(Items.Count > 0)
            {
                var container = Items[0];
                Items.RemoveAt(0);
                // If the item was an add, we can just get rid of it.
                if (container.Action != ChangeAction.Add)
                {
                    container.Action = ChangeAction.Remove;
                    Removed.Add(container);
                }
            }
        }

        /// <inheritdoc />
        public bool Contains(T item)
        {
            return Items.Any(c => Equals(c.Item, item));
        }

        /// <inheritdoc />
        public void CopyTo(T[] array, int arrayIndex)
        {
            foreach(var item in this)
            {
                array[arrayIndex++] = item;
            }
        }

        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator()
        {
            return Items.Select(c => c.Item).GetEnumerator();
        }

        /// <inheritdoc />
        public bool Remove(T item)
        {
            var index = Items.FindIndex(c => Equals(c.Item, item));
            if (index < 0)
                return false;

            var container = Items[index];
            Items.RemoveAt(index);
            // If the item being removed was added, that means we don't need to track it 
            // anymore, so the below does not apply.
            if (container.Action != ChangeAction.Add)
            {
                container.Action = ChangeAction.Remove;
                Removed.Add(container);
            }

            return true;
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <inheritdoc />
        public ChangeSet GetChangeSet(string path = "", bool commit = false)
        {
            var changes = new ChangeSet();
            // In order to have unique keys in the set, we add an integer to each
            // add/remove paths (key).  This value can be ignored during processing.
            var changeIndex = 1;
            foreach(var item in Items)
            {
                // Each ChangeContainerItem ChangeSet should only contain one item, so we get the first.
                var itemChange = item.GetChangeSet(path, commit).First();
                changes.Add($"{itemChange.Key}{changeIndex++}", itemChange.Value);
            }

            changeIndex = 1;
            foreach(var removed in Removed)
            {
                var removedChange = removed.GetChangeSet(path, commit).First();
                changes.Add($"{removedChange.Key}{changeIndex++}", removedChange.Value);
            }

            return changes;
        }

        /// <inheritdoc />
        public void Reset()
        {
            // Remove all adds and reset the rest.
            for(var i = 0; i < Items.Count; )
            {
                // NOTE: We don't increment variable "i" if an item is removed because it was an add.
                var item = Items[i];
                if (item.Action == ChangeAction.Add)
                    Items.RemoveAt(i);
                else
                {
                    item.Reset();
                    i++;
                }
            }

            // Move the removed back to the Items.
            foreach (var removed in Removed)
            {
                removed.Reset();
                // Just add the item back, order doesn't really matter.
                Items.Add(removed);
            }
        }
    }
}