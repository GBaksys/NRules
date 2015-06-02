using System;
using System.Collections.Generic;
using System.Linq;

namespace NRules.RuleModel.Aggregators
{
    /// <summary>
    /// Aggregator that groups matching facts into collections of elements with the same key.
    /// </summary>
    /// <typeparam name="TSource">Type of source elements to group.</typeparam>
    /// <typeparam name="TKey">Type of grouping key.</typeparam>
    /// <typeparam name="TElement">Type of elements to group.</typeparam>
    internal class GroupByAggregator<TSource, TKey, TElement> : IAggregator
    {
        private readonly Func<TSource, TKey> _keySelector;
        private readonly Func<TSource, TElement> _elementSelector;
        private readonly Dictionary<TKey, Grouping> _groups = new Dictionary<TKey, Grouping>();
        private readonly Dictionary<object, TKey> _sourceToKey = new Dictionary<object, TKey>(); 
        private readonly Dictionary<object, TElement> _sourceToElement = new Dictionary<object, TElement>(); 

        public GroupByAggregator(Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector)
        {
            _keySelector = keySelector;
            _elementSelector = elementSelector;
        }

        public IEnumerable<AggregationResult> Initial()
        {
            return AggregationResult.Empty;
        }

        public IEnumerable<AggregationResult> Add(object fact)
        {
            var source = (TSource) fact;
            var key = _keySelector(source);
            var element = _elementSelector(source);
            _sourceToKey[fact] = key;
            _sourceToElement[fact] = element;
            return Add(key, element);
        }

        public IEnumerable<AggregationResult> Modify(object fact)
        {
            var source = (TSource)fact;
            var key = _keySelector(source);
            var element = _elementSelector(source);
            var oldKey = _sourceToKey[fact];
            var oldElement = _sourceToElement[fact];
            _sourceToKey[fact] = key;
            _sourceToElement[fact] = element;

            if (Equals(key, oldKey) && Equals(element, oldElement))
            {
                return Modify(key, element);
            }

            var result1 = Remove(oldKey, oldElement);
            var result2 = Add(key, element);
            return result1.Union(result2);
        }

        public IEnumerable<AggregationResult> Remove(object fact)
        {
            var oldKey = _sourceToKey[fact];
            var oldElement = _sourceToElement[fact];
            _sourceToKey.Remove(fact);
            _sourceToElement.Remove(fact);
            return Remove(oldKey, oldElement);
        }

        private IEnumerable<AggregationResult> Add(TKey key, TElement element)
        {
            Grouping group;
            if (!_groups.TryGetValue(key, out group))
            {
                group = new Grouping(key);
                _groups[key] = group;

                group.Add(element);
                return new[] { AggregationResult.Added(group) };
            }

            group.Add(element);
            return new[] { AggregationResult.Modified(group) };
        }
        
        private IEnumerable<AggregationResult> Modify(TKey key, TElement element)
        {
            var group = _groups[key];
            group.Modify(element);

            return new[] { AggregationResult.Modified(group) };
        }
        
        private IEnumerable<AggregationResult> Remove(TKey key, TElement element)
        {
            var group = _groups[key];
            group.Remove(element);
            if (group.Count == 0)
            {
                _groups.Remove(key);
                return new[] { AggregationResult.Removed(group) };
            }

            return new[] { AggregationResult.Modified(group) };
        }

        public IEnumerable<object> Aggregates { get { return _groups.Values; } }

        private class Grouping : FactCollection<TElement>, IGrouping<TKey, TElement>
        {
            private readonly TKey _key;

            public Grouping(TKey key)
            {
                _key = key;
            }

            public TKey Key { get { return _key; } }
        }
    }
}