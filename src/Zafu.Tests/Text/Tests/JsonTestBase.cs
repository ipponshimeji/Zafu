using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Zafu.Testing;

namespace Zafu.Text.Tests {
	public abstract class JsonTestBase {
		#region types

		public class SampleObject: IEnumerable<KeyValuePair<string, object?>> {
			#region types

			public interface IPropertyObserver {
				void OnNull(string name);

				void OnBoolean(string name, bool value);

				void OnNumber(string name, double value);

				void OnString(string name, string? value);

				void OnObject<T>(string name, T value) where T: IEnumerable<KeyValuePair<string, object?>>;

				void OnArray<T>(string name, T value) where T: IEnumerable<object?>;

				void OnValue<T>(string name, T value);
			}

			protected class EnumeratorBuilder: IPropertyObserver {
				#region data

				private List<KeyValuePair<string, object?>> props = new List<KeyValuePair<string, object?>>();

				#endregion


				#region IPropertyObserver

				public virtual void OnNull(string name) {
					AddProperty(name, null);
				}

				public virtual void OnBoolean(string name, bool value) {
					AddProperty(name, value);
				}

				public virtual void OnNumber(string name, double value) {
					AddProperty(name, value);
				}

				public virtual void OnString(string name, string? value) {
					AddProperty(name, value);
				}

				public virtual void OnObject<T>(string name, T value) where T : IEnumerable<KeyValuePair<string, object?>> {
					AddProperty(name, value);
				}

				public virtual void OnArray<T>(string name, T value) where T : IEnumerable<object?> {
					AddProperty(name, value);
				}

				public virtual void OnValue<T>(string name, T value) {
					AddProperty(name, value);
				}

				#endregion


				#region methods

				protected void AddProperty(string name, object? value) {
					// check argument
					if (name == null) {
						throw new ArgumentNullException(nameof(name));
					}

					this.props.Add(new KeyValuePair<string, object?>(name, value));
				}

				public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() {
					return this.props.GetEnumerator();
				}

				#endregion
			}

			#endregion


			#region IEnumerator

			IEnumerator IEnumerable.GetEnumerator() {
				return this.GetEnumerator();
			}

			#endregion


			#region IEnumerable<KeyValuePair<string, object?>>

			public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() {
				EnumeratorBuilder enumBuilder = new EnumeratorBuilder();
				EnumerateProperties(enumBuilder);
				return enumBuilder.GetEnumerator();
			}

			#endregion


			#region overridables

			public virtual void EnumerateProperties(IPropertyObserver observer) {
			}

			#endregion
		}

		public class GeneralSampleObject: SampleObject {
			#region overrides

			public override void EnumerateProperties(IPropertyObserver observer) {
				// check argument
				Debug.Assert(observer != null);

				// enumerate properties
				// null
				observer.OnNull("null");
				// boolean
				observer.OnBoolean("boolean", true);
				// number
				observer.OnNumber("number", 3);
				// string
				observer.OnString("string", "a\"bc");
				// object
				observer.OnObject("object", new Dictionary<string, object?>() {
					{ "boolean", false },
					{ "number", 6.2 },
				});
				// array
				observer.OnArray("array", new object[] { true, 3.4567e-10 });
			}

			#endregion
		}


		public class SampleArray: IEnumerable<object?> {
			#region types

			public interface IItemObserver {
				void OnNull();

				void OnBoolean(bool value);

				void OnNumber(double value);

				void OnString(string? value);

				void OnObject<T>(T value) where T : IEnumerable<KeyValuePair<string, object?>>;

				void OnArray<T>(T value) where T : IEnumerable<object?>;

				void OnValue<T>(T value);
			}

			protected class EnumeratorBuilder: IItemObserver {
				#region data

				private List<object?> items = new List<object?>();

				#endregion


				#region IItemObserver

				public virtual void OnNull() {
					this.items.Add(null);
				}

				public virtual void OnBoolean( bool value) {
					this.items.Add(value);
				}

				public virtual void OnNumber(double value) {
					this.items.Add(value);
				}

				public virtual void OnString(string? value) {
					this.items.Add(value);
				}

				public virtual void OnObject<T>(T value) where T : IEnumerable<KeyValuePair<string, object?>> {
					this.items.Add(value);
				}

				public virtual void OnArray<T>(T value) where T : IEnumerable<object?> {
					this.items.Add(value);
				}

				public virtual void OnValue<T>(T value) {
					this.items.Add(value);
				}

				#endregion


				#region methods

				public IEnumerator<object?> GetEnumerator() {
					return this.items.GetEnumerator();
				}

				#endregion
			}

			#endregion


			#region IEnumerator

			IEnumerator IEnumerable.GetEnumerator() {
				return this.GetEnumerator();
			}

			#endregion


			#region IEnumerator<object?>

			public IEnumerator<object?> GetEnumerator() {
				EnumeratorBuilder enumBuilder = new EnumeratorBuilder();
				EnumerateItems(enumBuilder);
				return enumBuilder.GetEnumerator();
			}

			#endregion


			#region overridables

			public virtual void EnumerateItems(IItemObserver observer) {
			}

			#endregion
		}

		public class GeneralSampleArray: SampleArray {
			#region overrides

			public override void EnumerateItems(IItemObserver observer) {
				// check argument
				Debug.Assert(observer != null);

				// enumerate properties
				// null
				observer.OnNull();
				// boolean
				observer.OnBoolean(false);
				// number
				observer.OnNumber(123.45);
				// string
				observer.OnString("abc");
				// object
				observer.OnObject(new Dictionary<string, object?>() {
					{ "boolean", false },
					{ "number", -67 },
				});
				// array
				observer.OnArray(new object[] { true, 3.4567e-10, "123\\4" });
			}

			#endregion
		}

		#endregion


		#region samples

		public class ValueSample<T> {
			#region data

			public readonly T Value;

			public readonly string JsonText;

			#endregion


			#region constructor

			public ValueSample(T value, string jsonText) {
				// initialize members
				this.Value = value;
				this.JsonText = jsonText;
			}

			#endregion
		}

		public class StringValueSample: ValueSample<string?> {
			#region data

			/// <summary>
			/// Whether the <see cref="Value"/> contains any special character to be escaped.
			/// </summary>
			public readonly bool ContainsSpecialChar;

			#endregion


			#region constructor

			public StringValueSample(string? value, string jsonText, bool containsSpecialChar) : base(value, jsonText) {
				// initialize members
				this.ContainsSpecialChar = containsSpecialChar;
			}

			#endregion
		}

		public static IEnumerable<ValueSample<bool>> GetBooleanValueSamples() {
			return new ValueSample<bool>[] {
				//              (value, jsonText)
				new ValueSample<bool>(false, JsonUtil.JsonFalse),
				new ValueSample<bool>(true, JsonUtil.JsonTrue),
			};
		}

		public static IEnumerable<object[]> GetBooleanValueSampleData() {
			return GetBooleanValueSamples().ToTestData();
		}

		public static IEnumerable<ValueSample<double>> GetNumberValueSamples() {
			return new ValueSample<double>[] {
				//                (value, jsonText)
				// integer
				new ValueSample<double>(-123, "-123"),
				// floating point
				new ValueSample<double>(123.456, "123.456"),
				// floating point (exponential notation)
				new ValueSample<double>(1.23456e-12, "1.23456e-12"),
			};
		}

		public static IEnumerable<object[]> GetNumberValueSampleData() {
			return GetNumberValueSamples().ToTestData();
		}

		public static IEnumerable<StringValueSample> GetStringValueSamples() {
			return new StringValueSample[] {
				//              (value, jsonText, containsSpecialChar)
				// no special char
				new StringValueSample("abc123#-$-", "\"abc123#-$-\"", false),
				// single special char
				new StringValueSample("\"", "\"\\\"\"", true),
				new StringValueSample("\\abc", "\"\\\\abc\"", true),
				new StringValueSample("abc\n123", "\"abc\\n123\"", true),
				new StringValueSample("123\u0000", "\"123\\u0000\"", true),
				// multiple special chars
				new StringValueSample("\u0001\\", "\"\\u0001\\\\\"", true),
				new StringValueSample("\babc\f", "\"\\babc\\f\"", true),
				new StringValueSample("123\"\"abc\t+-#", "\"123\\\"\\\"abc\\t+-#\"", true),
				// null
				new StringValueSample(null, "null", false)
			};
		}

		public static IEnumerable<object[]> GetStringValueSampleData() {
			return GetStringValueSamples().ToTestData();
		}


		public static readonly SampleObject EmptyObjectValueSample = new SampleObject();

		public static readonly SampleObject GeneralObjectValueSample = new GeneralSampleObject();

		public static IEnumerable<SampleObject> GetObjectValueSamples() {
			return new SampleObject[] {
				EmptyObjectValueSample,
				GeneralObjectValueSample
			};
		}

		public static IEnumerable<object[]> GetObjectValueSampleData() {
			return GetObjectValueSamples().ToTestData();
		}


		public static readonly SampleArray EmptyArrayValueSample = new SampleArray();

		public static readonly SampleArray GeneralArrayValueSample = new GeneralSampleArray();

		public static IEnumerable<SampleArray> GetArrayValueSamples() {
			return new SampleArray[] {
				EmptyArrayValueSample,
				GeneralArrayValueSample
			};
		}

		public static IEnumerable<object[]> GetArrayValueSampleData() {
			return GetArrayValueSamples().ToTestData();
		}

		#endregion
	}
}
