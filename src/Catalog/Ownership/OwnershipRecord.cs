﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VDS.RDF;

namespace NuGet.Services.Metadata.Catalog.Ownership
{
    public class OwnershipRecord
    {
        public OwnershipRecord(Uri resourceUri, IGraph graph)
        {
            Base = resourceUri;
            Graph = graph ?? new Graph();
        }

        public Uri Base { get; private set; }
        public IGraph Graph { get; private set; }

        Uri GetRecordUri()
        {
            return new Uri(Base, "#record");
        }

        //  updates

        public void AddVersion(OwnershipRegistration registration, OwnershipOwner owner, string version)
        {
            INode registrationNode = Graph.CreateUriNode(registration.GetUri(Base));
            INode ownerNode = Graph.CreateUriNode(owner.GetUri(Base));
            INode recordNode = Graph.CreateUriNode(GetRecordUri());

            Graph.Assert(recordNode, Graph.CreateUriNode(Schema.Predicates.Type), Graph.CreateUriNode(Schema.DataTypes.Record));
            Graph.Assert(recordNode, Graph.CreateUriNode(Schema.Predicates.Registration), registrationNode);
            Graph.Assert(recordNode, Graph.CreateUriNode(Schema.Predicates.Owner), ownerNode);

            Graph.Assert(registrationNode, Graph.CreateUriNode(Schema.Predicates.Type), Graph.CreateUriNode(Schema.DataTypes.RecordRegistration));
            Graph.Assert(registrationNode, Graph.CreateUriNode(Schema.Predicates.Namespace), Graph.CreateLiteralNode(registration.Namespace));
            Graph.Assert(registrationNode, Graph.CreateUriNode(Schema.Predicates.Id), Graph.CreateLiteralNode(registration.Id));
            Graph.Assert(registrationNode, Graph.CreateUriNode(Schema.Predicates.Owner), ownerNode);

            if (version != null)
            {
                Graph.Assert(registrationNode, Graph.CreateUriNode(Schema.Predicates.Version), Graph.CreateLiteralNode(version));
            }

            Graph.Assert(ownerNode, Graph.CreateUriNode(Schema.Predicates.Type), Graph.CreateUriNode(Schema.DataTypes.RecordOwner));
            Graph.Assert(ownerNode, Graph.CreateUriNode(Schema.Predicates.NameIdentifier), Graph.CreateLiteralNode(owner.NameIdentifier));

            if (owner.Name != null)
            {
                Graph.Assert(ownerNode, Graph.CreateUriNode(Schema.Predicates.Name), Graph.CreateLiteralNode(owner.Name));
            }

            if (owner.GivenName != null)
            {
                Graph.Assert(ownerNode, Graph.CreateUriNode(Schema.Predicates.GivenName), Graph.CreateLiteralNode(owner.GivenName));
            }

            if (owner.Surname != null)
            {
                Graph.Assert(ownerNode, Graph.CreateUriNode(Schema.Predicates.Surname), Graph.CreateLiteralNode(owner.Surname));
            }

            if (owner.Email != null)
            {
                Graph.Assert(ownerNode, Graph.CreateUriNode(Schema.Predicates.Email), Graph.CreateLiteralNode(owner.Email));
            }

            if (owner.Iss != null)
            {
                Graph.Assert(ownerNode, Graph.CreateUriNode(Schema.Predicates.Iss), Graph.CreateLiteralNode(owner.Iss));
            }

            Graph.Assert(ownerNode, Graph.CreateUriNode(Schema.Predicates.Registration), registrationNode);
        }

        public void AddOwner(OwnershipRegistration registration, OwnershipOwner owner)
        {
            AddVersion(registration, owner, null);
        }

        public void RemoveRegistration(OwnershipRegistration registration)
        {
            foreach (Triple triple in Graph.GetTriples(Graph.CreateUriNode(registration.GetUri(Base))).ToList())
            {
                Graph.Retract(triple);
            }
        }

        public void RemoveVersion(OwnershipRegistration registration, string version)
        {
            Graph.Retract(
                Graph.CreateUriNode(registration.GetUri(Base)), 
                Graph.CreateUriNode(Schema.Predicates.Version), 
                Graph.CreateLiteralNode(version));
        }

        public void RemoveOwnerFromRegistration(OwnershipRegistration registration, OwnershipOwner owner)
        {
            Graph.Retract(
                Graph.CreateUriNode(registration.GetUri(Base)), 
                Graph.CreateUriNode(Schema.Predicates.Owner),
                Graph.CreateUriNode(owner.GetUri(Base)));
        }

        public void RemoveOwner(OwnershipOwner owner)
        {
            foreach (Triple triple in Graph.GetTriples(Graph.CreateUriNode(owner.GetUri(Base))).ToList())
            {
                Graph.Retract(triple);
            }
        }

        //  queries

        public bool HasRegistration(OwnershipRegistration registration)
        {
            return Graph.ContainsTriple(new Triple(
                Graph.CreateUriNode(GetRecordUri()),
                Graph.CreateUriNode(Schema.Predicates.Registration),
                Graph.CreateUriNode(registration.GetUri(Base))));
        }

        public bool HasVersion(OwnershipRegistration registration, string version)
        {
            return Graph.ContainsTriple(new Triple(
                Graph.CreateUriNode(registration.GetUri(Base)), 
                Graph.CreateUriNode(Schema.Predicates.Version), 
                Graph.CreateLiteralNode(version)));
        }

        public bool HasOwner(OwnershipRegistration registration, OwnershipOwner owner)
        {
            return Graph.ContainsTriple(new Triple(
                Graph.CreateUriNode(registration.GetUri(Base)),
                Graph.CreateUriNode(Schema.Predicates.Owner),
                Graph.CreateUriNode(owner.GetUri(Base))));
        }

        public IEnumerable<OwnershipRegistration> GetRegistrations(OwnershipOwner owner)
        {
            IList<OwnershipRegistration> result = new List<OwnershipRegistration>();

            foreach (Triple triple in Graph.GetTriplesWithSubjectPredicate(Graph.CreateUriNode(owner.GetUri(Base)), Graph.CreateUriNode(Schema.Predicates.Registration)))
            {
                string ns = Graph.GetTriplesWithSubjectPredicate(triple.Object, Graph.CreateUriNode(Schema.Predicates.Namespace)).First().Object.ToString();
                string id = Graph.GetTriplesWithSubjectPredicate(triple.Object, Graph.CreateUriNode(Schema.Predicates.Id)).First().Object.ToString();
                result.Add(new OwnershipRegistration { Namespace = ns, Id = id });
            }
            
            return result;
        }

        public IEnumerable<OwnershipOwner> GetOwners(OwnershipRegistration registration)
        {
            IList<OwnershipOwner> result = new List<OwnershipOwner>();

            foreach (Triple triple in Graph.GetTriplesWithSubjectPredicate(Graph.CreateUriNode(registration.GetUri(Base)), Graph.CreateUriNode(Schema.Predicates.Owner)))
            {
                string nameIdentifier = Graph.GetTriplesWithSubjectPredicate(triple.Object, Graph.CreateUriNode(Schema.Predicates.NameIdentifier)).First().Object.ToString();

                result.Add(new OwnershipOwner { NameIdentifier = nameIdentifier });
            }

            return result;
        }

        public IEnumerable<string> GetVersions(OwnershipRegistration registration)
        {
            IList<string> result = new List<string>();

            foreach (Triple triple in Graph.GetTriplesWithSubjectPredicate(Graph.CreateUriNode(registration.GetUri(Base)), Graph.CreateUriNode(Schema.Predicates.Version)))
            {
                string version = Graph.GetTriplesWithSubjectPredicate(triple.Object, Graph.CreateUriNode(Schema.Predicates.ObjectId)).First().Object.ToString();

                result.Add(version);
            }

            return result;
        }

        //  tenant support enable/disable/query

        public void EnableTenant(string tenant)
        {
            INode recordNode = Graph.CreateUriNode(GetRecordUri());
            Graph.Assert(recordNode, Graph.CreateUriNode(Schema.Predicates.Type), Graph.CreateUriNode(Schema.DataTypes.Record));
            Graph.Assert(recordNode, Graph.CreateUriNode(Schema.Predicates.Tenant), Graph.CreateLiteralNode(tenant));
        }

        public void DisableTenant(string tenant)
        {
            Graph.Retract(Graph.CreateUriNode(GetRecordUri()), Graph.CreateUriNode(Schema.Predicates.Tenant), Graph.CreateLiteralNode(tenant));
        }

        public bool HasTenantEnabled(string tenant)
        {
            return true;

            //return Graph.ContainsTriple(new Triple(
            //    Graph.CreateUriNode(GetRecordUri()),
            //    Graph.CreateUriNode(Schema.Predicates.Tenant),
            //    Graph.CreateLiteralNode(tenant)));
        }
    }
}
