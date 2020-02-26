// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Utilities;

namespace Microsoft.EntityFrameworkCore.Metadata.Internal
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public class Sequence : IMutableSequence, IConventionSequence
    {
        private readonly IModel _model;

        private string _name { get; set; }
        private string _schema { get; set; }
        private long? _startValue { get; set; }
        private int? _incrementBy { get; set; }
        private long? _minValue { get; set; }
        private long? _maxValue { get; set; }
        private Type _clrType { get; set; }
        private bool? _isCyclic { get; set; }

        private ConfigurationSource _configurationSource;
        private ConfigurationSource? _startValueConfigurationSource;
        private ConfigurationSource? _incrementByConfigurationSource;
        private ConfigurationSource? _minValueConfigurationSource;
        private ConfigurationSource? _maxValueConfigurationSource;
        private ConfigurationSource? _clrTypeConfigurationSource;
        private ConfigurationSource? _isCyclicConfigurationSource;

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public static readonly Type DefaultClrType = typeof(long);

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public const int DefaultIncrementBy = 1;

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public const int DefaultStartValue = 1;

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public static readonly long? DefaultMaxValue = default;

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public static readonly long? DefaultMinValue = default;

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public static readonly bool DefaultIsCyclic = default;

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public Sequence(
            [NotNull] IMutableModel model,
            [NotNull] string name,
            [CanBeNull] string schema,
            ConfigurationSource configurationSource)
        {
            Check.NotEmpty(name, nameof(name));
            Check.NullButNotEmpty(schema, nameof(schema));

            _model = model;
            _name = name;
            _schema = schema;
            _configurationSource = configurationSource;
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        [Obsolete("Use the other constructor")]
        public Sequence([NotNull] IModel model, [NotNull] string annotationName)
        {
            Check.NotNull(model, nameof(model));
            Check.NotEmpty(annotationName, nameof(annotationName));

            _model = model;
            _configurationSource = ConfigurationSource.Explicit;

            var data = SequenceData.Deserialize((string)model[annotationName]);
            _name = data.Name;
            _schema = data.Schema;
            _startValue = data.StartValue;
            _incrementBy = data.IncrementBy;
            _minValue = data.MinValue;
            _maxValue = data.MaxValue;
            _clrType = data.ClrType;
            _isCyclic = data.IsCyclic;
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public static IEnumerable<Sequence> GetSequences([NotNull] IModel model)
            => ((SortedDictionary<(string, string), Sequence>)model[RelationalAnnotationNames.Sequences])
                ?.Values ?? Enumerable.Empty<Sequence>();

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public static Sequence FindSequence([NotNull] IModel model, [NotNull] string name, [CanBeNull] string schema)
        {
            var sequences = (SortedDictionary<(string, string), Sequence>)model[RelationalAnnotationNames.Sequences];
            if (sequences == null
                || !sequences.TryGetValue((name, schema), out var sequence))
            {
                return null;
            }

            return sequence;
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public static Sequence AddSequence(
            [NotNull] IMutableModel model, [NotNull] string name, [CanBeNull] string schema, ConfigurationSource configurationSource)
        {
            var sequence = new Sequence(model, name, schema, configurationSource);
            var sequences = (SortedDictionary<(string, string), Sequence>)model[RelationalAnnotationNames.Sequences];
            if (sequences == null)
            {
                sequences = new SortedDictionary<(string, string), Sequence>();
                model[RelationalAnnotationNames.Sequences] = sequences;
            }

            sequences.Add((name, schema), sequence);
            return sequence;
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public static IMutableSequence RemoveSequence([NotNull] IMutableModel model, [NotNull] string name, [CanBeNull] string schema)
        {
            var sequences = (SortedDictionary<(string, string), Sequence>)model[RelationalAnnotationNames.Sequences];
            if (sequences == null
                || !sequences.TryGetValue((name, schema), out var sequence))
            {
                return null;
            }

            sequences.Remove((name, schema));

            return sequence;
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual IMutableModel Model => (IMutableModel)_model;

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        IConventionSequenceBuilder IConventionSequence.Builder => new SequenceBuilder(this);

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual string Name => _name;

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual string Schema => _schema ?? Model.GetDefaultSchema();

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual long StartValue
        {
            get => _startValue ?? DefaultStartValue;
            set => SetStartValue(value, ConfigurationSource.Explicit);
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual void SetStartValue(long? startValue, ConfigurationSource configurationSource)
        {
            _startValue = startValue;

            _startValueConfigurationSource = startValue == null
                ? (ConfigurationSource?)null
                : configurationSource.Max(_startValueConfigurationSource);
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual ConfigurationSource? GetStartValueConfigurationSource()
            => _startValueConfigurationSource;

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual int IncrementBy
        {
            get => _incrementBy ?? DefaultIncrementBy;
            set => SetIncrementBy(value, ConfigurationSource.Explicit);
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual void SetIncrementBy(int? incrementBy, ConfigurationSource configurationSource)
        {
            _incrementBy = incrementBy;

            _incrementByConfigurationSource = incrementBy == null
                ? (ConfigurationSource?)null
                : configurationSource.Max(_incrementByConfigurationSource);
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual ConfigurationSource? GetIncrementByConfigurationSource()
            => _incrementByConfigurationSource;

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual long? MinValue
        {
            get => _minValue ?? DefaultMinValue;
            set => SetMinValue(value, ConfigurationSource.Explicit);
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual void SetMinValue(long? minValue, ConfigurationSource configurationSource)
        {
            _minValue = minValue;

            _minValueConfigurationSource = minValue == null
                ? (ConfigurationSource?)null
                : configurationSource.Max(_minValueConfigurationSource);
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual ConfigurationSource? GetMinValueConfigurationSource()
            => _minValueConfigurationSource;

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual long? MaxValue
        {
            get => _maxValue;
            set => SetMaxValue(value, ConfigurationSource.Explicit);
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual void SetMaxValue(long? maxValue, ConfigurationSource configurationSource)
        {
            _maxValue = maxValue;

            _maxValueConfigurationSource = maxValue == null
                ? (ConfigurationSource?)null
                : configurationSource.Max(_maxValueConfigurationSource);
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual ConfigurationSource? GetMaxValueConfigurationSource()
            => _maxValueConfigurationSource;

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public static IReadOnlyCollection<Type> SupportedTypes { get; }
            = new[] { typeof(byte), typeof(long), typeof(int), typeof(short), typeof(decimal) };

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual Type ClrType
        {
            get => _clrType ?? DefaultClrType;
            set => SetClrType(value, ConfigurationSource.Explicit);
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual void SetClrType([CanBeNull] Type clrType, ConfigurationSource configurationSource)
        {
            if (clrType != null
                && !SupportedTypes.Contains(clrType))
            {
                throw new ArgumentException(RelationalStrings.BadSequenceType);
            }

            _clrType = clrType;

            _clrTypeConfigurationSource = clrType == null
                ? (ConfigurationSource?)null
                : configurationSource.Max(_clrTypeConfigurationSource);
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual ConfigurationSource? GetClrTypeConfigurationSource()
            => _clrTypeConfigurationSource;

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual bool IsCyclic
        {
            get => _isCyclic ?? DefaultIsCyclic;
            set => SetIsCyclic(value, ConfigurationSource.Explicit);
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual void SetIsCyclic(bool? cyclic, ConfigurationSource configurationSource)
        {
            _isCyclic = cyclic;

            _isCyclicConfigurationSource = cyclic == null
                ? (ConfigurationSource?)null
                : configurationSource.Max(_isCyclicConfigurationSource);
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual ConfigurationSource? GetIsCyclicConfigurationSource()
            => _isCyclicConfigurationSource;

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual ConfigurationSource GetConfigurationSource()
            => _configurationSource;

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual void UpdateConfigurationSource(ConfigurationSource configurationSource)
            => _configurationSource = _configurationSource.Max(configurationSource);

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        IConventionModel IConventionSequence.Model => (IConventionModel)Model;

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        IModel ISequence.Model => _model;

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        void IConventionSequence.SetStartValue(long? startValue, bool fromDataAnnotation)
            => SetStartValue(startValue, fromDataAnnotation ? ConfigurationSource.DataAnnotation : ConfigurationSource.Convention);

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        void IConventionSequence.SetIncrementBy(int? incrementBy, bool fromDataAnnotation)
            => SetIncrementBy(incrementBy, fromDataAnnotation ? ConfigurationSource.DataAnnotation : ConfigurationSource.Convention);

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        void IConventionSequence.SetMinValue(long? minValue, bool fromDataAnnotation)
            => SetMinValue(minValue, fromDataAnnotation ? ConfigurationSource.DataAnnotation : ConfigurationSource.Convention);

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        void IConventionSequence.SetMaxValue(long? maxValue, bool fromDataAnnotation)
            => SetMaxValue(maxValue, fromDataAnnotation ? ConfigurationSource.DataAnnotation : ConfigurationSource.Convention);

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        void IConventionSequence.SetClrType(Type clrType, bool fromDataAnnotation)
            => SetClrType(clrType, fromDataAnnotation ? ConfigurationSource.DataAnnotation : ConfigurationSource.Convention);

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        void IConventionSequence.SetIsCyclic(bool? cyclic, bool fromDataAnnotation)
            => SetIsCyclic(cyclic, fromDataAnnotation ? ConfigurationSource.DataAnnotation : ConfigurationSource.Convention);

        [Obsolete("Don't use this in any new code")]
        private sealed class SequenceData
        {
            public string Name { get; set; }

            public string Schema { get; set; }

            public long StartValue { get; set; }

            public int IncrementBy { get; set; }

            public long? MinValue { get; set; }

            public long? MaxValue { get; set; }

            public Type ClrType { get; set; }

            public bool IsCyclic { get; set; }

            public static SequenceData Deserialize([NotNull] string value)
            {
                Check.NotEmpty(value, nameof(value));

                try
                {
                    var data = new SequenceData();

                    // ReSharper disable PossibleInvalidOperationException
                    var position = 0;
                    data.Name = ExtractValue(value, ref position);
                    data.Schema = ExtractValue(value, ref position);
                    data.StartValue = (long)AsLong(ExtractValue(value, ref position));
                    data.IncrementBy = (int)AsLong(ExtractValue(value, ref position));
                    data.MinValue = AsLong(ExtractValue(value, ref position));
                    data.MaxValue = AsLong(ExtractValue(value, ref position));
                    data.ClrType = AsType(ExtractValue(value, ref position));
                    data.IsCyclic = AsBool(ExtractValue(value, ref position));
                    // ReSharper restore PossibleInvalidOperationException

                    return data;
                }
                catch (Exception ex)
                {
                    throw new ArgumentException(RelationalStrings.BadSequenceString, ex);
                }
            }

            private static string ExtractValue(string value, ref int position)
            {
                position = value.IndexOf('\'', position) + 1;

                var end = value.IndexOf('\'', position);

                while (end + 1 < value.Length
                    && value[end + 1] == '\'')
                {
                    end = value.IndexOf('\'', end + 2);
                }

                var extracted = value[position..end].Replace("''", "'");
                position = end + 1;

                return extracted.Length == 0 ? null : extracted;
            }

            private static long? AsLong(string value)
                => value == null ? null : (long?)long.Parse(value, CultureInfo.InvariantCulture);

            private static Type AsType(string value)
                => value == typeof(long).Name
                    ? typeof(long)
                    : value == typeof(int).Name
                        ? typeof(int)
                        : value == typeof(short).Name
                            ? typeof(short)
                            : value == typeof(decimal).Name
                                ? typeof(decimal)
                                : typeof(byte);

            private static bool AsBool(string value)
                => value != null && bool.Parse(value);

            private static void EscapeAndQuote(StringBuilder builder, object value)
            {
                builder.Append("'");

                if (value != null)
                {
                    builder.Append(value.ToString().Replace("'", "''"));
                }

                builder.Append("'");
            }
        }
    }
}
