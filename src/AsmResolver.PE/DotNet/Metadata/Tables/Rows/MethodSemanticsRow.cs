// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 3.0 of the License, or (at your option) any later version.
// 
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA

namespace AsmResolver.PE.DotNet.Metadata.Tables.Rows
{
    /// <summary>
    /// Represents a single row in the method semantics metadata table.
    /// </summary>
    public readonly struct MethodSemanticsRow : IMetadataRow
    {
        /// <summary>
        /// Reads a single method semantics row from an input stream.
        /// </summary>
        /// <param name="reader">The input stream.</param>
        /// <param name="layout">The layout of the method semantics table.</param>
        /// <returns>The row.</returns>
        public static MethodSemanticsRow FromReader(IBinaryStreamReader reader, TableLayout layout)
        {
            return new MethodSemanticsRow(
                (MethodSemanticsAttributes) reader.ReadUInt16(),
                reader.ReadIndex((IndexSize) layout.Columns[1].Size),
                reader.ReadIndex((IndexSize) layout.Columns[2].Size));
        }

        public MethodSemanticsRow(MethodSemanticsAttributes attributes, uint method, uint association)
        {
            Attributes = attributes;
            Method = method;
            Association = association;
        }
        
        /// <inheritdoc />
        public TableIndex TableIndex => TableIndex.MethodSemantics;

        /// <summary>
        /// Gets the semantic attributes that are assigned to the method.
        /// </summary>
        public MethodSemanticsAttributes Attributes
        {
            get;
        }

        /// <summary>
        /// Gets an index into the method definition table indicating the method that was assigned special semantics.
        /// </summary>
        public uint Method
        {
            get;
        }

        /// <summary>
        /// Gets a HasSemantics index (an index into either the event or property table) indicating the member the method
        /// is associated with.
        /// </summary>
        public uint Association
        {
            get;
        }

        /// <summary>
        /// Determines whether this row is considered equal to the provided method semantics row.
        /// </summary>
        /// <param name="other">The other row.</param>
        /// <returns><c>true</c> if the rows are equal, <c>false</c> otherwise.</returns>
        public bool Equals(MethodSemanticsRow other)
        {
            return Attributes == other.Attributes && Method == other.Method && Association == other.Association;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is MethodSemanticsRow other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (int) Attributes;
                hashCode = (hashCode * 397) ^ (int) Method;
                hashCode = (hashCode * 397) ^ (int) Association;
                return hashCode;
            }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"({(int) Attributes:X4}, {Method:X8}, {Association:X8})";
        }
        
    }
}