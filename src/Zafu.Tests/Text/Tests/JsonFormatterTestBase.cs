using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Xunit;

namespace Zafu.Text.Tests {
	public abstract class JsonFormatterTestBase: JsonTestBase {
		#region types

		protected class ChildElementWriter {
			#region data

			protected readonly TextWriter Writer;

			protected readonly IJsonFormatter Formatter;

			protected bool FirstItem = true;

			#endregion


			#region creation

			protected ChildElementWriter(TextWriter writer, IJsonFormatter formatter) {
				// check argument
				if (writer == null) {
					throw new ArgumentNullException(nameof(writer));
				}
				if (formatter == null) {
					throw new ArgumentNullException(nameof(formatter));
				}

				// initialize members
				this.Writer = writer;
				this.Formatter = formatter;
			}

			#endregion
		}

		protected class GenericObjectPropertyWriter: ChildElementWriter, SampleObject.IPropertyObserver {
			#region creation

			public GenericObjectPropertyWriter(TextWriter writer, IJsonFormatter formatter): base(writer, formatter) {
			}

			#endregion


			#region IPropertyObserver

			public virtual void OnNull(string name) {
				object? value = null;
				this.Formatter.WriteObjectProperty(this.Writer, name, value, ref this.FirstItem);
			}

			public virtual void OnBoolean(string name, bool value) {
				this.Formatter.WriteObjectProperty(this.Writer, name, value, ref this.FirstItem);
			}

			public virtual void OnNumber(string name, double value) {
				this.Formatter.WriteObjectProperty(this.Writer, name, value, ref this.FirstItem);
			}

			public virtual void OnString(string name, string? value) {
				this.Formatter.WriteObjectProperty(this.Writer, name, value, ref this.FirstItem);
			}

			public virtual void OnObject<T>(string name, T value) where T: IEnumerable<KeyValuePair<string, object?>> {
				this.Formatter.WriteObjectProperty<T>(this.Writer, name, value, ref this.FirstItem);
			}

			public virtual void OnArray<T>(string name, T value) where T : IEnumerable<object?> {
				this.Formatter.WriteObjectProperty<T>(this.Writer, name, value, ref this.FirstItem);
			}

			public virtual void OnValue<T>(string name, T value) {
				this.Formatter.WriteObjectProperty<T>(this.Writer, name, value, ref this.FirstItem);
			}

			#endregion
		}

		protected class ObjectPropertyWriter: GenericObjectPropertyWriter {
			#region creation

			public ObjectPropertyWriter(TextWriter writer, IJsonFormatter formatter) : base(writer, formatter) {
			}

			#endregion


			#region IPropertyObserver

			public override void OnNull(string name) {
				this.Formatter.WriteNullObjectProperty(this.Writer, name, ref this.FirstItem);
			}

			public override void OnBoolean(string name, bool value) {
				this.Formatter.WriteBooleanObjectProperty(this.Writer, name, value, ref this.FirstItem);
			}

			public override void OnNumber(string name, double value) {
				this.Formatter.WriteNumberObjectProperty(this.Writer, name, value, ref this.FirstItem);
			}

			public override void OnString(string name, string? value) {
				this.Formatter.WriteStringObjectProperty(this.Writer, name, value, ref this.FirstItem);
			}

			public override void OnObject<T>(string name, T value) {
				this.Formatter.WriteObjectObjectProperty<T>(this.Writer, name, value, ref this.FirstItem);
			}

			public override void OnArray<T>(string name, T value) {
				this.Formatter.WriteArrayObjectProperty<T>(this.Writer, name, value, ref this.FirstItem);
			}

			public override void OnValue<T>(string name, T value) {
				this.Formatter.WriteObjectProperty<T>(this.Writer, name, value, ref this.FirstItem);
			}

			#endregion
		}

		protected class GenericArrayItemWriter: ChildElementWriter, SampleArray.IItemObserver {
			#region creation

			public GenericArrayItemWriter(TextWriter writer, IJsonFormatter formatter) : base(writer, formatter) {
			}

			#endregion


			#region IItemObserver

			public virtual void OnNull() {
				object? value = null;
				this.Formatter.WriteArrayItem(this.Writer, value, ref this.FirstItem);
			}

			public virtual void OnBoolean(bool value) {
				this.Formatter.WriteArrayItem(this.Writer, value, ref this.FirstItem);
			}

			public virtual void OnNumber(double value) {
				this.Formatter.WriteArrayItem(this.Writer, value, ref this.FirstItem);
			}

			public virtual void OnString(string? value) {
				this.Formatter.WriteArrayItem(this.Writer, value, ref this.FirstItem);
			}

			public virtual void OnObject<T>(T value) where T : IEnumerable<KeyValuePair<string, object?>> {
				this.Formatter.WriteArrayItem<T>(this.Writer, value, ref this.FirstItem);
			}

			public virtual void OnArray<T>(T value) where T : IEnumerable<object?> {
				this.Formatter.WriteArrayItem<T>(this.Writer, value, ref this.FirstItem);
			}

			public virtual void OnValue<T>(T value) {
				this.Formatter.WriteArrayItem<T>(this.Writer, value, ref this.FirstItem);
			}

			#endregion
		}

		protected class ArrayItemWriter: GenericArrayItemWriter {
			#region creation

			public ArrayItemWriter(TextWriter writer, IJsonFormatter formatter) : base(writer, formatter) {
			}

			#endregion


			#region IItemObserver

			public override void OnNull() {
				this.Formatter.WriteNullArrayItem(this.Writer, ref this.FirstItem);
			}

			public override void OnBoolean(bool value) {
				this.Formatter.WriteBooleanArrayItem(this.Writer, value, ref this.FirstItem);
			}

			public override void OnNumber(double value) {
				this.Formatter.WriteNumberArrayItem(this.Writer, value, ref this.FirstItem);
			}

			public override void OnString(string? value) {
				this.Formatter.WriteStringArrayItem(this.Writer, value, ref this.FirstItem);
			}

			public override void OnObject<T>(T value) {
				this.Formatter.WriteObjectArrayItem<T>(this.Writer, value, ref this.FirstItem);
			}

			public override void OnArray<T>(T value) {
				this.Formatter.WriteArrayArrayItem<T>(this.Writer, value, ref this.FirstItem);
			}

			public override void OnValue<T>(T value) {
				this.Formatter.WriteArrayItem<T>(this.Writer, value, ref this.FirstItem);
			}

			#endregion
		}

		#endregion


		#region overridables

		protected abstract IJsonFormatter GetFormatter();

		public abstract string GetExpectedJsonText(object sample);

		#endregion


		#region tests

		[Fact(DisplayName = "WriteNull; general")]
		public void WriteNull_general() {
			// arrange
			IJsonFormatter formatter = GetFormatter();
			string expected = JsonUtil.JsonNull;

			// act
			string actual;
			using (StringWriter writer = new StringWriter()) {
				formatter.WriteNull(writer);
				actual = writer.ToString();
			}

			// assert
			Assert.Equal(expected, actual);
		}

		[Fact(DisplayName = "WriteNull; writer: null")]
		public void WriteNull_writer_null() {
			// arrange
			IJsonFormatter formatter = GetFormatter();
			TextWriter writer = null!;

			// act
			ArgumentNullException actual = Assert.Throws<ArgumentNullException>(() => {
				formatter.WriteNull(writer);
			});

			// assert
			Assert.Equal("writer", actual.ParamName);
		}

		[Theory(DisplayName = "WriteBoolean; general")]
		[MemberData(nameof(GetBooleanValueSampleData))]
		public void WriteBoolean_general(ValueSample<bool> sample) {
			// arrange
			IJsonFormatter formatter = GetFormatter();

			// act
			string actual;
			using (StringWriter writer = new StringWriter()) {
				formatter.WriteBoolean(writer, sample.Value);
				actual = writer.ToString();
			}

			// assert
			Assert.Equal(sample.JsonText, actual);
		}

		[Fact(DisplayName = "WriteBoolean; writer: null")]
		public void WriteBoolean_writer_null() {
			// arrange
			IJsonFormatter formatter = GetFormatter();
			TextWriter writer = null!;
			bool value = default(bool);

			// act
			ArgumentNullException actual = Assert.Throws<ArgumentNullException>(() => {
				formatter.WriteBoolean(writer, value);
			});

			// assert
			Assert.Equal("writer", actual.ParamName);
		}

		[Theory(DisplayName = "WriteNumber; general")]
		[MemberData(nameof(GetNumberValueSampleData))]
		public void WriteNumber_general(ValueSample<double> sample) {
			// arrange
			IJsonFormatter formatter = GetFormatter();

			// act
			string actual;
			using (StringWriter writer = new StringWriter()) {
				formatter.WriteNumber(writer, sample.Value);
				actual = writer.ToString();
			}

			// assert
			Assert.Equal(sample.JsonText, actual);
		}

		[Fact(DisplayName = "WriteNumber; writer: null")]
		public void WriteNumber_writer_null() {
			// arrange
			IJsonFormatter formatter = GetFormatter();
			TextWriter writer = null!;
			double value = default(double);

			// act
			ArgumentNullException actual = Assert.Throws<ArgumentNullException>(() => {
				formatter.WriteNumber(writer, value);
			});

			// assert
			Assert.Equal("writer", actual.ParamName);
		}

		[Theory(DisplayName = "WriteString; general")]
		[MemberData(nameof(GetStringValueSampleData))]
		public void WriteString_general(StringValueSample sample) {
			// arrange
			IJsonFormatter formatter = GetFormatter();

			// act
			string actual;
			using (StringWriter writer = new StringWriter()) {
				formatter.WriteString(writer, sample.Value);
				actual = writer.ToString();
			}

			// assert
			Assert.Equal(sample.JsonText, actual);
		}

		[Fact(DisplayName = "WriteString; writer: null")]
		public void WriteString_writer_null() {
			// arrange
			IJsonFormatter formatter = GetFormatter();
			TextWriter writer = null!;
			string value = string.Empty;

			// act
			ArgumentNullException actual = Assert.Throws<ArgumentNullException>(() => {
				formatter.WriteString(writer, value);
			});

			// assert
			Assert.Equal("writer", actual.ParamName);
		}

		[Theory(DisplayName = "WriteObject; general")]
		[MemberData(nameof(GetObjectValueSampleData))]
		public void WriteObject_general(SampleObject sample) {
			// arrange
			IJsonFormatter formatter = GetFormatter();
			string expected = GetExpectedJsonText(sample);

			// act
			string actual;
			using (StringWriter writer = new StringWriter()) {
				formatter.WriteObject(writer, sample);
				actual = writer.ToString();
			}

			// assert
			Assert.Equal(expected, actual);
		}

		[Fact(DisplayName = "WriteObject; writer: null")]
		public void WriteObject_writer_null() {
			// arrange
			IJsonFormatter formatter = GetFormatter();
			TextWriter writer = null!;
			SampleObject value = EmptyObjectValueSample;

			// act
			ArgumentNullException actual = Assert.Throws<ArgumentNullException>(() => {
				formatter.WriteObject(writer, value);
			});

			// assert
			Assert.Equal("writer", actual.ParamName);
		}

		[Theory(DisplayName = "WriteArray; general")]
		[MemberData(nameof(GetArrayValueSampleData))]
		public void WriteArray_general(SampleArray sample) {
			// arrange
			IJsonFormatter formatter = GetFormatter();
			string expected = GetExpectedJsonText(sample);

			// act
			string actual;
			using (StringWriter writer = new StringWriter()) {
				formatter.WriteArray(writer, sample);
				actual = writer.ToString();
			}

			// assert
			Assert.Equal(expected, actual);
		}

		[Fact(DisplayName = "WriteArray; writer: null")]
		public void WriteArray_writer_null() {
			// arrange
			IJsonFormatter formatter = GetFormatter();
			TextWriter writer = null!;
			SampleArray value = EmptyArrayValueSample;

			// act
			ArgumentNullException actual = Assert.Throws<ArgumentNullException>(() => {
				formatter.WriteArray(writer, value);
			});

			// assert
			Assert.Equal("writer", actual.ParamName);
		}

		[Fact(DisplayName = "WriteValue; value: null")]
		public void WriteValue_value_null() {
			// arrange
			IJsonFormatter formatter = GetFormatter();
			object? value = null;
			string expected = JsonUtil.JsonNull;

			// act
			string actual;
			using (StringWriter writer = new StringWriter()) {
				formatter.WriteValue(writer, value);
				actual = writer.ToString();
			}

			// assert
			Assert.Equal(expected, actual);
		}

		[Theory(DisplayName = "WriteValue; value: bool")]
		[MemberData(nameof(GetBooleanValueSampleData))]
		public void WriteValue_value_bool(ValueSample<bool> sample) {
			// arrange
			IJsonFormatter formatter = GetFormatter();

			// act
			string actual;
			using (StringWriter writer = new StringWriter()) {
				formatter.WriteValue(writer, sample.Value);
				actual = writer.ToString();
			}

			// assert
			Assert.Equal(sample.JsonText, actual);
		}

		[Theory(DisplayName = "WriteValue; value: double")]
		[MemberData(nameof(GetNumberValueSampleData))]
		public void WriteValue_value_double(ValueSample<double> sample) {
			// arrange
			IJsonFormatter formatter = GetFormatter();

			// act
			string actual;
			using (StringWriter writer = new StringWriter()) {
				formatter.WriteValue(writer, sample.Value);
				actual = writer.ToString();
			}

			// assert
			Assert.Equal(sample.JsonText, actual);
		}

		[Theory(DisplayName = "WriteValue; value: string")]
		[MemberData(nameof(GetStringValueSampleData))]
		public void WriteValue_value_string(StringValueSample sample) {
			// arrange
			IJsonFormatter formatter = GetFormatter();

			// act
			string actual;
			using (StringWriter writer = new StringWriter()) {
				formatter.WriteValue(writer, sample.Value);
				actual = writer.ToString();
			}

			// assert
			Assert.Equal(sample.JsonText, actual);
		}

		[Theory(DisplayName = "WriteValue; value: object")]
		[MemberData(nameof(GetObjectValueSampleData))]
		public void WriteValue_value_object(SampleObject sample) {
			// arrange
			IJsonFormatter formatter = GetFormatter();
			string expected = GetExpectedJsonText(sample);

			// act
			string actual;
			using (StringWriter writer = new StringWriter()) {
				formatter.WriteValue(writer, sample);
				actual = writer.ToString();
			}

			// assert
			Assert.Equal(expected, actual);
		}

		[Theory(DisplayName = "WriteValue; value: array")]
		[MemberData(nameof(GetArrayValueSampleData))]
		public void WriteValue_value_array(SampleArray sample) {
			// arrange
			IJsonFormatter formatter = GetFormatter();
			string expected = GetExpectedJsonText(sample);

			// act
			string actual;
			using (StringWriter writer = new StringWriter()) {
				formatter.WriteValue(writer, sample);
				actual = writer.ToString();
			}

			// assert
			Assert.Equal(expected, actual);
		}

		[Fact(DisplayName = "WriteValue; value: IConvertible")]
		public void WriteValue_value_IConvertible() {
			// arrange
			IJsonFormatter formatter = GetFormatter();
			Decimal value = -567.89m;
			#pragma warning disable CS0183
			Debug.Assert(value is IConvertible);
			#pragma warning restore CS0183
			string expected = ((IConvertible)value).ToDouble(CultureInfo.InvariantCulture).ToString();

			// act
			string actual;
			using (StringWriter writer = new StringWriter()) {
				formatter.WriteValue(writer, value);
				actual = writer.ToString();
			}

			// assert
			Assert.Equal(expected, actual);
		}

		[Fact(DisplayName = "WriteValue; writer: null")]
		public void WriteValue_writer_null() {
			// arrange
			IJsonFormatter formatter = GetFormatter();
			TextWriter writer = null!;
			bool value = false;

			// act
			ArgumentNullException actual = Assert.Throws<ArgumentNullException>(() => {
				formatter.WriteValue(writer, value);
			});

			// assert
			Assert.Equal("writer", actual.ParamName);
		}

		[Fact(DisplayName = "writing object properties (non-generic)")]
		public void WritingObject_NonGeneric() {
			// It try to write contents of the general object sample using the following methods:
			// WriteObjectStart, WriteObjectEnd, WriteObjectPropertyHeader, and WriteXObjectProperty.

			// arrange
			IJsonFormatter formatter = GetFormatter();
			SampleObject sample = GeneralObjectValueSample;
			string expected = GetExpectedJsonText(sample);

			// act
			string actual;
			using (StringWriter writer = new StringWriter()) {
				ObjectPropertyWriter propWriter = new ObjectPropertyWriter(writer, formatter);

				formatter.WriteObjectStart(writer);
				sample.EnumerateProperties(propWriter);
				formatter.WriteObjectEnd(writer);

				actual = writer.ToString();
			}

			// assert
			Assert.Equal(expected, actual);
		}

		[Fact(DisplayName = "writing object properties (generic)")]
		public void WritingObject_Generic() {
			// It try to write contents of the general object sample using the following methods:
			// WriteObjectStart, WriteObjectEnd, WriteObjectPropertyHeader, and WriteObjectProperty<T>.

			// arrange
			IJsonFormatter formatter = GetFormatter();
			SampleObject sample = GeneralObjectValueSample;
			string expected = GetExpectedJsonText(sample);

			// act
			string actual;
			using (StringWriter writer = new StringWriter()) {
				GenericObjectPropertyWriter propWriter = new GenericObjectPropertyWriter(writer, formatter);

				formatter.WriteObjectStart(writer);
				sample.EnumerateProperties(propWriter);
				formatter.WriteObjectEnd(writer);

				actual = writer.ToString();
			}

			// assert
			Assert.Equal(expected, actual);
		}

		[Fact(DisplayName = "writing array items (non-generic)")]
		public void WritingArray_NonGeneric() {
			// It try to write contents of the general array sample using the following methods:
			// WriteArrayStart, WriteArrayEnd, WriteArrayItemHeader, and WriteXArrayItem.

			// arrange
			IJsonFormatter formatter = GetFormatter();
			SampleArray sample = GeneralArrayValueSample;
			string expected = GetExpectedJsonText(sample);

			// act
			string actual;
			using (StringWriter writer = new StringWriter()) {
				ArrayItemWriter itemWriter = new ArrayItemWriter(writer, formatter);

				formatter.WriteArrayStart(writer);
				sample.EnumerateItems(itemWriter);
				formatter.WriteArrayEnd(writer);

				actual = writer.ToString();
			}

			// assert
			Assert.Equal(expected, actual);
		}

		[Fact(DisplayName = "writing array items (generic)")]
		public void WritingArray_Generic() {
			// It try to write contents of the general array sample using the following methods:
			// WriteArrayStart, WriteArrayEnd, WriteArrayItemHeader, and WriteArrayItem<T>.

			// arrange
			IJsonFormatter formatter = GetFormatter();
			SampleArray sample = GeneralArrayValueSample;
			string expected = GetExpectedJsonText(sample);

			// act
			string actual;
			using (StringWriter writer = new StringWriter()) {
				GenericArrayItemWriter itemWriter = new GenericArrayItemWriter(writer, formatter);

				formatter.WriteArrayStart(writer);
				sample.EnumerateItems(itemWriter);
				formatter.WriteArrayEnd(writer);

				actual = writer.ToString();
			}

			// assert
			Assert.Equal(expected, actual);
		}

		#endregion
	}
}
