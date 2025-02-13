﻿/*
 Code based on https://github.com/microsoft/referencesource/blob/master/System.Numerics/System/Numerics/Quaternion.cs
 From the repository https://github.com/microsoft/referencesource
 Copyright (c) Microsoft. All rights reserved.
 Licensed under the MIT license. See LICENSE file in the project root for full license information.
*/

using System.Globalization;
using Newtonsoft.Json;
using Parsec.Extensions;
using Parsec.Readers;
using Parsec.Shaiya.Core;

namespace Parsec.Shaiya.Common;

/// <summary>
/// A structure encapsulating a four-dimensional vector (x,y,z,w),
/// which is used to efficiently rotate an object about the (x,y,z) vector by the angle theta, where w = cos(theta/2).
/// </summary>
public struct Quaternion : IEquatable<Quaternion>, IBinary
{
    /// <summary>
    /// Specifies the X-value of the vector component of the Quaternion.
    /// </summary>
    public float X;

    /// <summary>
    /// Specifies the Y-value of the vector component of the Quaternion.
    /// </summary>
    public float Y;

    /// <summary>
    /// Specifies the Z-value of the vector component of the Quaternion.
    /// </summary>
    public float Z;

    /// <summary>
    /// Specifies the rotation component of the Quaternion.
    /// </summary>
    public float W;

    public Quaternion(SBinaryReader binaryReader)
    {
        X = binaryReader.Read<float>();
        Y = binaryReader.Read<float>();
        Z = binaryReader.Read<float>();
        W = binaryReader.Read<float>();
    }

    /// <summary>
    /// Returns a Quaternion representing no rotation.
    /// </summary>
    public static Quaternion Identity
    {
        get { return new Quaternion(0, 0, 0, 1); }
    }

    /// <summary>
    /// Returns whether the Quaternion is the identity Quaternion.
    /// </summary>
    [JsonIgnore]
    public bool IsIdentity
    {
        get { return X == 0f && Y == 0f && Z == 0f && W == 1f; }
    }

    /// <summary>
    /// Constructs a Quaternion from the given components.
    /// </summary>
    /// <param name="x">The X component of the Quaternion.</param>
    /// <param name="y">The Y component of the Quaternion.</param>
    /// <param name="z">The Z component of the Quaternion.</param>
    /// <param name="w">The W component of the Quaternion.</param>
    public Quaternion(float x, float y, float z, float w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }

    /// <summary>
    /// Returns the inverse of a Quaternion.
    /// </summary>
    /// <param name="value">The source Quaternion.</param>
    /// <returns>The inverted Quaternion.</returns>
    public static Quaternion Inverse(Quaternion value)
    {
        //  -1   (       a              -v       )
        // q   = ( -------------   ------------- )
        //       (  a^2 + |v|^2  ,  a^2 + |v|^2  )

        Quaternion ans;

        float ls = value.X * value.X + value.Y * value.Y + value.Z * value.Z + value.W * value.W;
        float invNorm = 1.0f / ls;

        ans.X = -value.X * invNorm;
        ans.Y = -value.Y * invNorm;
        ans.Z = -value.Z * invNorm;
        ans.W = value.W * invNorm;

        return ans;
    }

    /// <summary>
    /// Creates a Quaternion from the given rotation matrix.
    /// </summary>
    /// <param name="matrix">The rotation matrix.</param>
    /// <returns>The created Quaternion.</returns>
    public static Quaternion CreateFromRotationMatrix(Matrix4x4 matrix)
    {
        float trace = matrix.M11 + matrix.M22 + matrix.M33;

        Quaternion q = new Quaternion();

        if (trace > 0.0f)
        {
            float s = (float)Math.Sqrt(trace + 1.0f);
            q.W = s * 0.5f;
            s = 0.5f / s;
            q.X = (matrix.M23 - matrix.M32) * s;
            q.Y = (matrix.M31 - matrix.M13) * s;
            q.Z = (matrix.M12 - matrix.M21) * s;
        }
        else
        {
            if (matrix.M11 >= matrix.M22 && matrix.M11 >= matrix.M33)
            {
                float s = (float)Math.Sqrt(1.0f + matrix.M11 - matrix.M22 - matrix.M33);
                float invS = 0.5f / s;
                q.X = 0.5f * s;
                q.Y = (matrix.M12 + matrix.M21) * invS;
                q.Z = (matrix.M13 + matrix.M31) * invS;
                q.W = (matrix.M23 - matrix.M32) * invS;
            }
            else if (matrix.M22 > matrix.M33)
            {
                float s = (float)Math.Sqrt(1.0f + matrix.M22 - matrix.M11 - matrix.M33);
                float invS = 0.5f / s;
                q.X = (matrix.M21 + matrix.M12) * invS;
                q.Y = 0.5f * s;
                q.Z = (matrix.M32 + matrix.M23) * invS;
                q.W = (matrix.M31 - matrix.M13) * invS;
            }
            else
            {
                float s = (float)Math.Sqrt(1.0f + matrix.M33 - matrix.M11 - matrix.M22);
                float invS = 0.5f / s;
                q.X = (matrix.M31 + matrix.M13) * invS;
                q.Y = (matrix.M32 + matrix.M23) * invS;
                q.Z = 0.5f * s;
                q.W = (matrix.M12 - matrix.M21) * invS;
            }
        }

        return q;
    }

    /// <summary>
    /// Calculates the dot product of two Quaternions.
    /// </summary>
    /// <param name="quaternion1">The first source Quaternion.</param>
    /// <param name="quaternion2">The second source Quaternion.</param>
    /// <returns>The dot product of the Quaternions.</returns>
    public static float Dot(Quaternion quaternion1, Quaternion quaternion2)
    {
        return quaternion1.X * quaternion2.X + quaternion1.Y * quaternion2.Y + quaternion1.Z * quaternion2.Z +
               quaternion1.W * quaternion2.W;
    }

    /// <summary>
    /// Interpolates between two quaternions, using spherical linear interpolation.
    /// </summary>
    /// <param name="quaternion1">The first source Quaternion.</param>
    /// <param name="quaternion2">The second source Quaternion.</param>
    /// <param name="amount">The relative weight of the second source Quaternion in the interpolation.</param>
    /// <returns>The interpolated Quaternion.</returns>
    public static Quaternion Slerp(Quaternion quaternion1, Quaternion quaternion2, float amount)
    {
        const float epsilon = 1e-6f;

        float t = amount;

        float cosOmega = quaternion1.X * quaternion2.X +
                         quaternion1.Y * quaternion2.Y +
                         quaternion1.Z * quaternion2.Z +
                         quaternion1.W * quaternion2.W;

        bool flip = false;

        if (cosOmega < 0.0f)
        {
            flip = true;
            cosOmega = -cosOmega;
        }

        float s1, s2;

        if (cosOmega > 1.0f - epsilon)
        {
            // Too close, do straight linear interpolation.
            s1 = 1.0f - t;
            s2 = flip ? -t : t;
        }
        else
        {
            float omega = (float)Math.Acos(cosOmega);
            float invSinOmega = (float)(1 / Math.Sin(omega));

            s1 = (float)Math.Sin((1.0f - t) * omega) * invSinOmega;
            s2 = flip ? (float)-Math.Sin(t * omega) * invSinOmega : (float)Math.Sin(t * omega) * invSinOmega;
        }

        Quaternion ans;

        ans.X = s1 * quaternion1.X + s2 * quaternion2.X;
        ans.Y = s1 * quaternion1.Y + s2 * quaternion2.Y;
        ans.Z = s1 * quaternion1.Z + s2 * quaternion2.Z;
        ans.W = s1 * quaternion1.W + s2 * quaternion2.W;

        return ans;
    }

    /// <summary>
    ///  Linearly interpolates between two quaternions.
    /// </summary>
    /// <param name="quaternion1">The first source Quaternion.</param>
    /// <param name="quaternion2">The second source Quaternion.</param>
    /// <param name="amount">The relative weight of the second source Quaternion in the interpolation.</param>
    /// <returns>The interpolated Quaternion.</returns>
    public static Quaternion Lerp(Quaternion quaternion1, Quaternion quaternion2, float amount)
    {
        float t = amount;
        float t1 = 1.0f - t;

        Quaternion r = new Quaternion();

        float dot = quaternion1.X * quaternion2.X + quaternion1.Y * quaternion2.Y + quaternion1.Z * quaternion2.Z +
                    quaternion1.W * quaternion2.W;

        if (dot >= 0.0f)
        {
            r.X = t1 * quaternion1.X + t * quaternion2.X;
            r.Y = t1 * quaternion1.Y + t * quaternion2.Y;
            r.Z = t1 * quaternion1.Z + t * quaternion2.Z;
            r.W = t1 * quaternion1.W + t * quaternion2.W;
        }
        else
        {
            r.X = t1 * quaternion1.X - t * quaternion2.X;
            r.Y = t1 * quaternion1.Y - t * quaternion2.Y;
            r.Z = t1 * quaternion1.Z - t * quaternion2.Z;
            r.W = t1 * quaternion1.W - t * quaternion2.W;
        }

        // Normalize it.
        float ls = r.X * r.X + r.Y * r.Y + r.Z * r.Z + r.W * r.W;
        float invNorm = 1.0f / (float)Math.Sqrt(ls);

        r.X *= invNorm;
        r.Y *= invNorm;
        r.Z *= invNorm;
        r.W *= invNorm;

        return r;
    }

    /// <summary>
    /// Flips the sign of each component of the quaternion.
    /// </summary>
    /// <param name="value">The source Quaternion.</param>
    /// <returns>The negated Quaternion.</returns>
    public static Quaternion Negate(Quaternion value)
    {
        Quaternion ans;

        ans.X = -value.X;
        ans.Y = -value.Y;
        ans.Z = -value.Z;
        ans.W = -value.W;

        return ans;
    }

    /// <summary>
    /// Adds two Quaternions element-by-element.
    /// </summary>
    /// <param name="value1">The first source Quaternion.</param>
    /// <param name="value2">The second source Quaternion.</param>
    /// <returns>The result of adding the Quaternions.</returns>
    public static Quaternion Add(Quaternion value1, Quaternion value2)
    {
        Quaternion ans;

        ans.X = value1.X + value2.X;
        ans.Y = value1.Y + value2.Y;
        ans.Z = value1.Z + value2.Z;
        ans.W = value1.W + value2.W;

        return ans;
    }

    /// <summary>
    /// Subtracts one Quaternion from another.
    /// </summary>
    /// <param name="value1">The first source Quaternion.</param>
    /// <param name="value2">The second Quaternion, to be subtracted from the first.</param>
    /// <returns>The result of the subtraction.</returns>
    public static Quaternion Subtract(Quaternion value1, Quaternion value2)
    {
        Quaternion ans;

        ans.X = value1.X - value2.X;
        ans.Y = value1.Y - value2.Y;
        ans.Z = value1.Z - value2.Z;
        ans.W = value1.W - value2.W;

        return ans;
    }

    /// <summary>
    /// Multiplies two Quaternions together.
    /// </summary>
    /// <param name="value1">The Quaternion on the left side of the multiplication.</param>
    /// <param name="value2">The Quaternion on the right side of the multiplication.</param>
    /// <returns>The result of the multiplication.</returns>
    public static Quaternion Multiply(Quaternion value1, Quaternion value2)
    {
        Quaternion ans;

        float q1x = value1.X;
        float q1y = value1.Y;
        float q1z = value1.Z;
        float q1w = value1.W;

        float q2x = value2.X;
        float q2y = value2.Y;
        float q2z = value2.Z;
        float q2w = value2.W;

        // cross(av, bv)
        float cx = q1y * q2z - q1z * q2y;
        float cy = q1z * q2x - q1x * q2z;
        float cz = q1x * q2y - q1y * q2x;

        float dot = q1x * q2x + q1y * q2y + q1z * q2z;

        ans.X = q1x * q2w + q2x * q1w + cx;
        ans.Y = q1y * q2w + q2y * q1w + cy;
        ans.Z = q1z * q2w + q2z * q1w + cz;
        ans.W = q1w * q2w - dot;

        return ans;
    }

    /// <summary>
    /// Multiplies a Quaternion by a scalar value.
    /// </summary>
    /// <param name="value1">The source Quaternion.</param>
    /// <param name="value2">The scalar value.</param>
    /// <returns>The result of the multiplication.</returns>
    public static Quaternion Multiply(Quaternion value1, float value2)
    {
        Quaternion ans;

        ans.X = value1.X * value2;
        ans.Y = value1.Y * value2;
        ans.Z = value1.Z * value2;
        ans.W = value1.W * value2;

        return ans;
    }

    /// <summary>
    /// Divides a Quaternion by another Quaternion.
    /// </summary>
    /// <param name="value1">The source Quaternion.</param>
    /// <param name="value2">The divisor.</param>
    /// <returns>The result of the division.</returns>
    public static Quaternion Divide(Quaternion value1, Quaternion value2)
    {
        Quaternion ans;

        float q1x = value1.X;
        float q1y = value1.Y;
        float q1z = value1.Z;
        float q1w = value1.W;

        //-------------------------------------
        // Inverse part.
        float ls = value2.X * value2.X + value2.Y * value2.Y + value2.Z * value2.Z + value2.W * value2.W;
        float invNorm = 1.0f / ls;

        float q2x = -value2.X * invNorm;
        float q2y = -value2.Y * invNorm;
        float q2z = -value2.Z * invNorm;
        float q2w = value2.W * invNorm;

        //-------------------------------------
        // Multiply part.

        // cross(av, bv)
        float cx = q1y * q2z - q1z * q2y;
        float cy = q1z * q2x - q1x * q2z;
        float cz = q1x * q2y - q1y * q2x;

        float dot = q1x * q2x + q1y * q2y + q1z * q2z;

        ans.X = q1x * q2w + q2x * q1w + cx;
        ans.Y = q1y * q2w + q2y * q1w + cy;
        ans.Z = q1z * q2w + q2z * q1w + cz;
        ans.W = q1w * q2w - dot;

        return ans;
    }

    /// <summary>
    /// Flips the sign of each component of the quaternion.
    /// </summary>
    /// <param name="value">The source Quaternion.</param>
    /// <returns>The negated Quaternion.</returns>
    public static Quaternion operator -(Quaternion value)
    {
        Quaternion ans;

        ans.X = -value.X;
        ans.Y = -value.Y;
        ans.Z = -value.Z;
        ans.W = -value.W;

        return ans;
    }

    /// <summary>
    /// Adds two Quaternions element-by-element.
    /// </summary>
    /// <param name="value1">The first source Quaternion.</param>
    /// <param name="value2">The second source Quaternion.</param>
    /// <returns>The result of adding the Quaternions.</returns>
    public static Quaternion operator +(Quaternion value1, Quaternion value2)
    {
        Quaternion ans;

        ans.X = value1.X + value2.X;
        ans.Y = value1.Y + value2.Y;
        ans.Z = value1.Z + value2.Z;
        ans.W = value1.W + value2.W;

        return ans;
    }

    /// <summary>
    /// Subtracts one Quaternion from another.
    /// </summary>
    /// <param name="value1">The first source Quaternion.</param>
    /// <param name="value2">The second Quaternion, to be subtracted from the first.</param>
    /// <returns>The result of the subtraction.</returns>
    public static Quaternion operator -(Quaternion value1, Quaternion value2)
    {
        Quaternion ans;

        ans.X = value1.X - value2.X;
        ans.Y = value1.Y - value2.Y;
        ans.Z = value1.Z - value2.Z;
        ans.W = value1.W - value2.W;

        return ans;
    }

    /// <summary>
    /// Multiplies two Quaternions together.
    /// </summary>
    /// <param name="value1">The Quaternion on the left side of the multiplication.</param>
    /// <param name="value2">The Quaternion on the right side of the multiplication.</param>
    /// <returns>The result of the multiplication.</returns>
    public static Quaternion operator *(Quaternion value1, Quaternion value2)
    {
        Quaternion ans;

        float q1x = value1.X;
        float q1y = value1.Y;
        float q1z = value1.Z;
        float q1w = value1.W;

        float q2x = value2.X;
        float q2y = value2.Y;
        float q2z = value2.Z;
        float q2w = value2.W;

        // cross(av, bv)
        float cx = q1y * q2z - q1z * q2y;
        float cy = q1z * q2x - q1x * q2z;
        float cz = q1x * q2y - q1y * q2x;

        float dot = q1x * q2x + q1y * q2y + q1z * q2z;

        ans.X = q1x * q2w + q2x * q1w + cx;
        ans.Y = q1y * q2w + q2y * q1w + cy;
        ans.Z = q1z * q2w + q2z * q1w + cz;
        ans.W = q1w * q2w - dot;

        return ans;
    }

    /// <summary>
    /// Multiplies a Quaternion by a scalar value.
    /// </summary>
    /// <param name="value1">The source Quaternion.</param>
    /// <param name="value2">The scalar value.</param>
    /// <returns>The result of the multiplication.</returns>
    public static Quaternion operator *(Quaternion value1, float value2)
    {
        Quaternion ans;

        ans.X = value1.X * value2;
        ans.Y = value1.Y * value2;
        ans.Z = value1.Z * value2;
        ans.W = value1.W * value2;

        return ans;
    }

    /// <summary>
    /// Divides a Quaternion by another Quaternion.
    /// </summary>
    /// <param name="value1">The source Quaternion.</param>
    /// <param name="value2">The divisor.</param>
    /// <returns>The result of the division.</returns>
    public static Quaternion operator /(Quaternion value1, Quaternion value2)
    {
        Quaternion ans;

        float q1x = value1.X;
        float q1y = value1.Y;
        float q1z = value1.Z;
        float q1w = value1.W;

        //-------------------------------------
        // Inverse part.
        float ls = value2.X * value2.X + value2.Y * value2.Y + value2.Z * value2.Z + value2.W * value2.W;
        float invNorm = 1.0f / ls;

        float q2x = -value2.X * invNorm;
        float q2y = -value2.Y * invNorm;
        float q2z = -value2.Z * invNorm;
        float q2w = value2.W * invNorm;

        //-------------------------------------
        // Multiply part.

        // cross(av, bv)
        float cx = q1y * q2z - q1z * q2y;
        float cy = q1z * q2x - q1x * q2z;
        float cz = q1x * q2y - q1y * q2x;

        float dot = q1x * q2x + q1y * q2y + q1z * q2z;

        ans.X = q1x * q2w + q2x * q1w + cx;
        ans.Y = q1y * q2w + q2y * q1w + cy;
        ans.Z = q1z * q2w + q2z * q1w + cz;
        ans.W = q1w * q2w - dot;

        return ans;
    }

    /// <summary>
    /// Returns a boolean indicating whether the two given Quaternions are equal.
    /// </summary>
    /// <param name="value1">The first Quaternion to compare.</param>
    /// <param name="value2">The second Quaternion to compare.</param>
    /// <returns>True if the Quaternions are equal; False otherwise.</returns>
    public static bool operator ==(Quaternion value1, Quaternion value2)
    {
        return value1.X == value2.X && value1.Y == value2.Y && value1.Z == value2.Z && value1.W == value2.W;
    }

    /// <summary>
    /// Returns a boolean indicating whether the two given Quaternions are not equal.
    /// </summary>
    /// <param name="value1">The first Quaternion to compare.</param>
    /// <param name="value2">The second Quaternion to compare.</param>
    /// <returns>True if the Quaternions are not equal; False if they are equal.</returns>
    public static bool operator !=(Quaternion value1, Quaternion value2)
    {
        return value1.X != value2.X || value1.Y != value2.Y || value1.Z != value2.Z || value1.W != value2.W;
    }

    /// <summary>
    /// Returns a boolean indicating whether the given Quaternion is equal to this Quaternion instance.
    /// </summary>
    /// <param name="other">The Quaternion to compare this instance to.</param>
    /// <returns>True if the other Quaternion is equal to this instance; False otherwise.</returns>
    public bool Equals(Quaternion other)
    {
        return X == other.X && Y == other.Y && Z == other.Z && W == other.W;
    }

    /// <summary>
    /// Returns a boolean indicating whether the given Object is equal to this Quaternion instance.
    /// </summary>
    /// <param name="obj">The Object to compare against.</param>
    /// <returns>True if the Object is equal to this Quaternion; False otherwise.</returns>
    public override bool Equals(object obj)
    {
        if (obj is Quaternion)
        {
            return Equals((Quaternion)obj);
        }

        return false;
    }

    /// <summary>
    /// Returns a String representing this Quaternion instance.
    /// </summary>
    /// <returns>The string representation.</returns>
    public override string ToString()
    {
        CultureInfo ci = CultureInfo.CurrentCulture;

        return String.Format(ci, "{{X:{0} Y:{1} Z:{2} W:{3}}}", X.ToString(ci), Y.ToString(ci), Z.ToString(ci), W.ToString(ci));
    }

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode()
    {
        return X.GetHashCode() + Y.GetHashCode() + Z.GetHashCode() + W.GetHashCode();
    }

    public IEnumerable<byte> GetBytes(params object[] options)
    {
        var buffer = new List<byte>();

        buffer.AddRange(X.GetBytes());
        buffer.AddRange(Y.GetBytes());
        buffer.AddRange(Z.GetBytes());
        buffer.AddRange(W.GetBytes());

        return buffer;
    }
}
