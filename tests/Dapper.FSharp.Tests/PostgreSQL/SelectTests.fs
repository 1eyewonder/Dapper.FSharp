﻿module Dapper.FSharp.Tests.PostgreSQL.SelectTests

open System
open System.Threading
open System.Threading.Tasks
open NUnit.Framework
open Dapper.FSharp.PostgreSQL
open Dapper.FSharp.Tests.Database

[<TestFixture>]
[<NonParallelizable>]
type SelectTests () =
    
    let personsView = table'<Persons.View> "Persons"
    let dogsView = table'<Dogs.View> "Dogs"
    let dogsWeightsView = table'<DogsWeights.View> "DogsWeights"
    let conn = Database.getConnection()
    let init = Database.getInitializer conn
    
    [<OneTimeSetUp>]
    member _.``Setup DB``() = conn |> Database.safeInit
        
    [<Test>]
    member _.``Selects by single where condition``() =
        task {
            do! init.InitPersons()
            let rs = Persons.View.generate 10
            let! _ =
                insert {
                    into personsView
                    values rs
                } |> conn.InsertAsync
                
            let! fromDb =
                select {
                    for p in personsView do
                    where (p.Position = 5)
                } |> conn.SelectAsync<Persons.View>
            
            Assert.AreEqual (rs |> List.find (fun x -> x.Position = 5), Seq.head fromDb)
        }
    
    [<Test>]
    member _.``Cancellation works`` () =
        task {
            do! init.InitPersons()
            let rs = Persons.View.generate 10
            let! _ =
                insert {
                    into personsView
                    values rs
                } |> conn.InsertAsync

            use cts = new CancellationTokenSource()
            cts.Cancel()
            let selectCrud query =
                conn.SelectAsync<Persons.View>(query, cancellationToken = cts.Token) :> Task
            let action () = 
                select {
                    for p in personsView do
                    where (p.Position = 5)
                } |> selectCrud
            
            Assert.ThrowsAsync<OperationCanceledException>(action) |> ignore
        }
        
    [<Test>]
    member _.``Selects by single where condition with table name used``() =
        task {
            do! init.InitPersons()
            let rs = Persons.View.generate 10
            let! _ =
                insert {
                    into personsView
                    values rs
                } |> conn.InsertAsync
            let! fromDb =
                select {
                    for p in personsView do
                    where (p.Position = 5)
                } |> conn.SelectAsync<Persons.View>
            
            Assert.AreEqual(rs |> List.find (fun x -> x.Position = 5), Seq.head fromDb)
        }
        
    [<Test>]
    member _.``Selects by IN where condition`` () =
        task {
            do! init.InitPersons()
            let rs = Persons.View.generate 10
            let! _ =
                insert {
                    into personsView
                    values rs
                } |> conn.InsertAsync
            let! fromDb =
                select {
                    for p in personsView do
                    where (isIn p.Position [5;6])
                    orderBy p.Position
                } |> conn.SelectAsync<Persons.View>
            
            let exp1 = rs |> List.find (fun x -> x.Position = 5)
            let act1 = Seq.head fromDb
            Assert.AreEqual(exp1,act1)
            
            let exp2 = rs |> List.find (fun x -> x.Position = 6)
            let act2 = Seq.last fromDb
            Assert.AreEqual(exp2,act2)
        }
        
    [<Test>]
    member _.``Selects by NOT IN where condition``() =
        task {
            do! init.InitPersons()
            let rs = Persons.View.generate 10
            let! _ =
                insert {
                    into personsView
                    values rs
                } |> conn.InsertAsync
            let! fromDb =
                select {
                    for p in personsView do
                    where (isNotIn p.Position [1;2;3])
                    orderBy p.Position
                } |> conn.SelectAsync<Persons.View>
            
            Assert.AreEqual(rs |> List.find (fun x -> x.Position = 4), Seq.head fromDb)
            Assert.AreEqual(rs |> List.find (fun x -> x.Position = 10), Seq.last fromDb)
            Assert.AreEqual(7, Seq.length fromDb)
        }
        
    [<Test>]
    member _.``Selects by IS NULL where condition``() =
        task {
            do! init.InitPersons()
            let rs = Persons.View.generate 10
            let! _ =
                insert {
                    into personsView
                    values rs
                } |> conn.InsertAsync
            let! fromDb =
                select {
                    for p in personsView do
                    where (p.DateOfBirth = None)
                    orderBy p.Position
                } |> conn.SelectAsync<Persons.View>
            
            Assert.AreEqual (rs |> List.find (fun x -> x.Position = 2), Seq.head fromDb)
            Assert.AreEqual (5, Seq.length fromDb)
    }
        
    [<Test>]
    member _.``Selects by IS NOT NULL where condition``() =
        task {
            do! init.InitPersons()
            let rs = Persons.View.generate 10
            let! _ =
                insert {
                    into personsView
                    values rs
                } |> conn.InsertAsync
            let! fromDb =
                select {
                    for p in personsView do
                    where (p.DateOfBirth <> None)
                    orderBy p.Position
                } |> conn.SelectAsync<Persons.View>
            
            Assert.AreEqual (rs |> List.find (fun x -> x.Position = 1), Seq.head fromDb)
            Assert.AreEqual (5, Seq.length fromDb)
        }
    
    [<Test>]
    member _.``Selects by LIKE where condition return matching rows``() =
        task {
            do! init.InitPersons()
            let rs = Persons.View.generate 10
            let! _ =
                insert {
                    into personsView
                    values rs
                } |> conn.InsertAsync
            let! fromDb =
                select {
                    for p in personsView do
                    where (like p.FirstName "First_1%")
                } |> conn.SelectAsync<Persons.View>
            
            CollectionAssert.IsNotEmpty fromDb
            Assert.AreEqual(2, Seq.length fromDb)
            Assert.IsTrue(fromDb |> Seq.forall (fun (p:Persons.View) -> p.FirstName.StartsWith "First"))
        }
    
    [<Test>]
    member _.``Selects by NOT LIKE where condition return matching rows``() =
        task {
            do! init.InitPersons()
            let rs = Persons.View.generate 10
            let! _ =
                insert {
                    into personsView
                    values rs
                } |> conn.InsertAsync
            let! fromDb =
                select {
                    for p in personsView do
                    where (notLike p.FirstName "First_1%")
                }
                |> conn.SelectAsync<Persons.View>
            
            Assert.AreEqual(8, Seq.length fromDb)
        }
        
    [<Test>]
    member _.``Selects by NOT LIKE where condition do not return non-matching rows``() =
        task {
            do! init.InitPersons()
            let rs = Persons.View.generate 10
            let! _ =
                insert {
                    into personsView
                    values rs
                } |> conn.InsertAsync
            let! fromDb =
                select {
                    for p in personsView do
                    where (notLike p.FirstName "NonExistingName%")
                } |> conn.SelectAsync<Persons.View>
            
            Assert.AreEqual(10, Seq.length fromDb)
        }
    
    [<Test>]
    member _.``Selects by UNARY NOT where condition``() =
        task {
            do! init.InitPersons()
            let rs = Persons.View.generate 10
            let! _ =
                insert {
                    into personsView
                    values rs
                } |> conn.InsertAsync
            let! fromDb =
                select {
                    for p in personsView do
                    where (not(p.Position > 5 && p.DateOfBirth = None))
                    orderBy p.Position
                } |> conn.SelectAsync<Persons.View>
            
            Assert.AreEqual(rs |> List.find (fun x -> x.Position = 9), Seq.last fromDb)
            Assert.AreEqual(7, Seq.length fromDb)
        }
        
    [<Test>]
    member _.``Selects by multiple where conditions``() =
        task {
            do! init.InitPersons()
            let rs = Persons.View.generate 10
            let! _ =
                insert {
                    into personsView
                    values rs
                } |> conn.InsertAsync
            let! fromDb =
                select {
                    for p in personsView do
                    where (p.Position > 2 && p.Position < 4)
                } |> conn.SelectAsync<Persons.View>
            
            Assert.AreEqual (rs |> List.find (fun x -> x.Position = 3), Seq.head fromDb)
        }
    
    [<Test>]
    member _.``Selects with order by``() =
        task {
            do! init.InitPersons()
            let rs = Persons.View.generate 10
            let! _ =
                insert {
                    into personsView
                    values rs
                } |> conn.InsertAsync
            let! fromDb =
                select {
                    for p in personsView do
                    orderByDescending p.Position
                } |> conn.SelectAsync<Persons.View>
            
            Assert.AreEqual(10, fromDb |> Seq.head |> (fun (x:Persons.View) -> x.Position))
        }
    
    [<Test>]
    member _.``Selects with skip parameter``() =
        task {
            do! init.InitPersons()
            let rs = Persons.View.generate 10
            let! _ =
                insert {
                    into personsView
                    values rs
                } |> conn.InsertAsync
            let! fromDb =
                select {
                    for p in personsView do
                    skip 5
                    orderBy p.Position
                } |> conn.SelectAsync<Persons.View>
            
            Assert.AreEqual(6, fromDb |> Seq.head |> (fun (x:Persons.View) -> x.Position))
            Assert.AreEqual(5, fromDb |> Seq.length)
        }
    
    [<Test>]
    member _.``Selects with skipTake parameter``() =
        task {
            do! init.InitPersons()
            let rs = Persons.View.generate 10
            let! _ =
                insert {
                    into personsView
                    values rs
                } |> conn.InsertAsync
            let! fromDb =
                select {
                    for p in personsView do
                    skipTake 5 2
                    orderBy p.Position
                } |> conn.SelectAsync<Persons.View>
            
            Assert.AreEqual(6, fromDb |> Seq.head |> (fun (x:Persons.View) -> x.Position))
            Assert.AreEqual(2, fromDb |> Seq.length)
        }
    
    [<Test>]
    member _.``Selects with skip and take parameters``() =
        task {
            do! init.InitPersons()
            let rs = Persons.View.generate 10
            let! _ =
                insert {
                    into personsView
                    values rs
                } |> conn.InsertAsync
            let! fromDb =
                select {
                    for p in personsView do
                    skip 5
                    take 2
                    orderBy p.Position
                } |> conn.SelectAsync<Persons.View>
            
            Assert.AreEqual(6, fromDb |> Seq.head |> (fun (x:Persons.View) -> x.Position))
            Assert.AreEqual(2, fromDb |> Seq.length)
        }
    
    [<Test>]
    member _.``Selects with one inner join - 1:1``() =
        task {
            do! init.InitPersons()
            do! init.InitDogs()

            let persons = Persons.View.generate 10
            let dogs = Dogs.View.generate1to1 persons
            let! _ =
                insert {
                    into personsView
                    values persons
                } |> conn.InsertAsync
            let! _ =
                insert {
                    into dogsView
                    values dogs
                } |> conn.InsertAsync
            let! fromDb =
                select {
                    for p in personsView do
                    innerJoin d in dogsView on (p.Id = d.OwnerId)
                    selectAll
                } |> conn.SelectAsync<Persons.View, Dogs.View>

            Assert.AreEqual(10, Seq.length fromDb)
            Assert.AreEqual((persons.Head, dogs.Head), (Seq.head fromDb))
        }
    
    [<Test>]
    member _.``Selects with one inner join - 1:N``() =
        task {
            do! init.InitPersons()
            do! init.InitDogs()
            let persons = Persons.View.generate 10
            let dogs = Dogs.View.generate1toN 5 persons.Head
            let! _ =
                insert {
                    into personsView
                    values persons
                } |> conn.InsertAsync
            let! _ =
                insert {
                    into dogsView
                    values dogs
                } |> conn.InsertAsync
            let! fromDb =
                select {
                    for p in personsView do
                    innerJoin d in dogsView on (p.Id = d.OwnerId)
                    selectAll
                } |> conn.SelectAsync<Persons.View, Dogs.View>

            let byOwner = fromDb |> Seq.groupBy fst

            Assert.AreEqual(5, Seq.length fromDb)
            Assert.AreEqual((persons.Head, dogs.Head), (Seq.head fromDb))
            Assert.AreEqual(1, Seq.length byOwner)
            Assert.AreEqual(5, byOwner |> Seq.head |> snd |> Seq.length)
        }
        
    [<Test>]
    member _.``Selects with one left join``() =
        task {
            do! init.InitPersons()
            do! init.InitDogs()
            let persons = Persons.View.generate 10
            let dogs = Dogs.View.generate1toN 5 persons.Head
            let! _ =
                insert {
                    into personsView
                    values persons
                } |> conn.InsertAsync
            let! _ =
                insert {
                    into dogsView
                    values dogs
                } |> conn.InsertAsync
            let! fromDb =
                select {
                    for p in personsView do
                    leftJoin d in dogsView on (p.Id = d.OwnerId)
                    orderBy p.Position
                    thenBy d.Nickname
                } |> conn.SelectAsyncOption<Persons.View, Dogs.View>

            let byOwner = fromDb |> Seq.groupBy fst

            Assert.AreEqual(14, Seq.length fromDb)
            Assert.AreEqual(5, byOwner |> Seq.head |> snd |> Seq.length)
            Assert.IsTrue (fromDb |> Seq.last |> snd |> Option.isNone)
            Assert.AreEqual((dogs |> List.head |> Some), (fromDb |> Seq.head |> snd))
        }
    
    [<Test>]
    member _.``Selects with two inner joins - 1:1``() =
        task {
            do! init.InitPersons()
            do! init.InitDogs()
            do! init.InitDogsWeights()

            let persons = Persons.View.generate 10
            let dogs = Dogs.View.generate1to1 persons
            let weights = DogsWeights.View.generate1to1 dogs

            let! _ =
                insert {
                    into personsView
                    values persons
                } |> conn.InsertAsync
            let! _ =
                insert {
                    into dogsView
                    values dogs
                } |> conn.InsertAsync
            let! _ =
                insert {
                    into dogsWeightsView
                    values weights
                } |> conn.InsertAsync

            let! fromDb =
                select {
                    for p in personsView do
                    innerJoin d in dogsView on (p.Id = d.OwnerId)
                    innerJoin dw in dogsWeightsView on (d.Nickname = dw.DogNickname)
                    orderBy p.Position
                }
                |> conn.SelectAsync<Persons.View, Dogs.View, DogsWeights.View>

            Assert.AreEqual(10, Seq.length fromDb)
            Assert.AreEqual((persons.Head, dogs.Head, weights.Head), (Seq.head fromDb))
        }
    
    [<Test>]
    member _.``Selects with two inner joins - 1:N``() =
        task {
            do! init.InitPersons()
            do! init.InitDogs()
            do! init.InitDogsWeights()

            let persons = Persons.View.generate 10
            let dogs = Dogs.View.generate1toN 5 persons.Head
            let weights = DogsWeights.View.generate1toN 3 dogs.Head

            let! _ =
                insert {
                    into personsView
                    values persons
                } |> conn.InsertAsync
            let! _ =
                insert {
                    into dogsView
                    values dogs
                } |> conn.InsertAsync
            let! _ =
                insert {
                    into dogsWeightsView
                    values weights
                } |> conn.InsertAsync

            let! fromDb =
                select {
                    for p in personsView do
                    innerJoin d in dogsView on (p.Id = d.OwnerId)
                    innerJoin dw in dogsWeightsView on (d.Nickname = dw.DogNickname)
                    orderBy p.Position
                    thenBy d.Nickname
                    thenBy dw.Year
                } |> conn.SelectAsync<Persons.View, Dogs.View, DogsWeights.View>

            Assert.AreEqual(3, Seq.length fromDb)
            Assert.AreEqual((persons.Head, dogs.Head, weights.Head), Seq.head fromDb)
        }
    
    [<Test>]
    member _.``Selects with two left joins``() =
        task {
            do! init.InitPersons()
            do! init.InitDogs()
            do! init.InitDogsWeights()

            let persons = Persons.View.generate 10
            let dogs = Dogs.View.generate1toN 5 persons.Head
            let weights = DogsWeights.View.generate1toN 3 dogs.Head

            let! _ =
                insert {
                    into personsView
                    values persons
                } |> conn.InsertAsync
            let! _ =
                insert {
                    into dogsView
                    values dogs
                } |> conn.InsertAsync
            let! _ =
                insert {
                    into dogsWeightsView
                    values weights
                } |> conn.InsertAsync

            let! fromDb =
                select {
                    for p in personsView do
                    leftJoin d in dogsView on (p.Id = d.OwnerId)
                    leftJoin dw in dogsWeightsView on (d.Nickname = dw.DogNickname)
                    orderBy p.Position
                    thenBy d.Nickname
                    thenBy dw.Year
                } |> conn.SelectAsyncOption<Persons.View, Dogs.View, DogsWeights.View>

            let p1,d1,w1 = fromDb |> Seq.head
            Assert.AreEqual(persons.Head, p1)
            Assert.AreEqual(Some dogs.Head, d1)
            Assert.AreEqual(Some weights.Head, w1)

            let pn,dn,wn = fromDb |> Seq.last
            Assert.AreEqual((persons |> Seq.last), pn)
            Assert.AreEqual(None, dn)
            Assert.AreEqual(None, wn)
            Assert.AreEqual(16, Seq.length fromDb)
        }