﻿[<AutoOpen>]
module Dapper.FSharp.PostgreSQL.Deconstructor

open Dapper.FSharp.PostgreSQL
open Dapper.FSharp.PostgreSQL.Evaluator

[<AbstractClass;Sealed>]
type Deconstructor =
    static member select<'a> (q:SelectQuery) = q |> GenericDeconstructor.select1<'a> evalSelectQuery
    static member select<'a,'b> (q:SelectQuery) = q |> GenericDeconstructor.select2<'a,'b> evalSelectQuery
    static member select<'a,'b,'c> (q:SelectQuery) = q |> GenericDeconstructor.select3<'a,'b,'c> evalSelectQuery
    static member insert (q:InsertQuery<'a>) = q |> GenericDeconstructor.insert evalInsertQuery
    static member insertOutput<'Input, 'Output> (q:InsertQuery<'Input>) = q |> GenericDeconstructor.insertOutput<'Input, 'Output> evalInsertQuery
    static member update<'a> (q:UpdateQuery<'a>) = q |> GenericDeconstructor.update<'a> evalUpdateQuery
    static member updateOutput<'Input, 'Output> (q:UpdateQuery<'Input>) = q |> GenericDeconstructor.updateOutput<'Input, 'Output> evalUpdateQuery
    static member delete (q:DeleteQuery) = q |> GenericDeconstructor.delete evalDeleteQuery
    static member deleteOutput<'Output> (q:DeleteQuery) = q |> GenericDeconstructor.deleteOutput<'Output> evalDeleteQuery