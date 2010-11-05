﻿/* Copyright 2010 10gen Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace MongoDB.Bson.DefaultSerializer {
    public class ObjectSerializer : BsonBaseSerializer {
        #region private static fields
        private static ObjectSerializer singleton = new ObjectSerializer();
        #endregion

        #region constructors
        private ObjectSerializer() {
        }
        #endregion

        #region public static properties
        public static ObjectSerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(object), singleton);
        }
        #endregion

        #region public methods
        public override object DeserializeDocument(
            BsonReader bsonReader,
            Type nominalType
        ) {
            if (nominalType != typeof(object)) {
                var message = string.Format("ObjectSerializer called for type: {0}", nominalType.FullName);
                throw new InvalidOperationException(message);
            }

            // if it's not an empty document it should have a discriminator
            bsonReader.PushBookmark();
            bsonReader.ReadStartDocument();
            Type actualType = nominalType;
            if (bsonReader.HasElement()) {
                var discriminatorConvention = BsonDefaultSerializer.LookupDiscriminatorConvention(nominalType);
                actualType = discriminatorConvention.GetActualDocumentType(bsonReader, nominalType);
                if (actualType == nominalType) {
                    throw new BsonSerializationException("Unable to determine actual type of document to deserialize");
                }
            }
            bsonReader.PopBookmark();

            if (actualType == nominalType) {
                bsonReader.ReadStartDocument();
                bsonReader.ReadEndDocument();
                return new object();
            } else {
                var serializer = BsonSerializer.LookupSerializer(actualType);
                return serializer.DeserializeDocument(bsonReader, nominalType);
            }
        }

        public override void SerializeDocument(
            BsonWriter bsonWriter,
            Type nominalType,
            object document,
            bool serializeIdFirst
        ) {
            var actualType = document.GetType();
            if (actualType != typeof(object)) {
                var message = string.Format("ObjectSerializer called for type: {0}", actualType.FullName);
                throw new InvalidOperationException(message);
            }

            bsonWriter.WriteStartDocument();
            bsonWriter.WriteEndDocument();
        }
        #endregion
    }
}