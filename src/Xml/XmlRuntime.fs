﻿// --------------------------------------------------------------------------------------
// XML type provider - methods & types used by the generated erased code
// --------------------------------------------------------------------------------------
namespace FSharp.Data

open System.Xml.Linq
open System.Runtime.InteropServices

// XElementExtensions is not a static class with C#-style extension methods because that would
// force to reference System.Xml.Linq.dll everytime you reference FSharp.Data, even when not using
// any of the XML parts
[<AutoOpen>]
/// Extension methods for XElement. It is auto opened.
module XElementExtensions = 

    type XElement with

      /// Sends the XML to the specified uri. Defaults to a POST request.
      member x.Request(uri:string, [<Optional>] ?httpMethod, [<Optional>] ?headers:seq<_>) =  
        let httpMethod = defaultArg httpMethod HttpMethod.Post
        let headers = defaultArg (Option.map List.ofSeq headers) []
        let headers =
            if headers |> List.exists (fst >> ((=) (fst (HttpRequestHeaders.UserAgent ""))))
            then headers
            else HttpRequestHeaders.UserAgent "F# Data XML Type Provider" :: headers
        let headers = HttpRequestHeaders.ContentType HttpContentTypes.Xml :: headers
        Http.Request(
          uri,
          body = TextRequest (x.ToString(SaveOptions.DisableFormatting)),
          headers = headers,
          httpMethod = httpMethod)

      /// Sends the XML to the specified uri. Defaults to a POST request.
      member x.Request(uri:string, [<Optional>] ?httpMethod, [<Optional>] ?headers:seq<_>) =
        let httpMethod = defaultArg httpMethod HttpMethod.Post
        let headers = defaultArg (Option.map List.ofSeq headers) []
        let headers =
            if headers |> List.exists (fst >> ((=) (fst (HttpRequestHeaders.UserAgent ""))))
            then headers
            else HttpRequestHeaders.UserAgent "F# Data XML Type Provider" :: headers
        let headers = HttpRequestHeaders.ContentType HttpContentTypes.Xml :: headers
        Http.AsyncRequest(
          uri,
          body = TextRequest (x.ToString(SaveOptions.DisableFormatting)),
          headers = headers,
          httpMethod = httpMethod)

namespace FSharp.Data.Runtime

open System
open System.ComponentModel
open System.IO
open System.Xml.Linq

#nowarn "10001"

/// Underlying representation of types generated by XmlProvider
[<StructuredFormatDisplay("{_Print}")>]
type XmlElement = 
  
  // NOTE: Using a record here to hide the ToString, GetHashCode & Equals
  // (but since this is used across multiple files, we have explicit Create method)
  { XElement : XElement }
  
  /// [omit]
  [<EditorBrowsableAttribute(EditorBrowsableState.Never)>]
  [<CompilerMessageAttribute("This method is intended for use in generated code only.", 10001, IsHidden=true, IsError=false)>]
  member x._Print = x.XElement.ToString()

  /// [omit]
  [<EditorBrowsableAttribute(EditorBrowsableState.Never)>]
  [<CompilerMessageAttribute("This method is intended for use in generated code only.", 10001, IsHidden=true, IsError=false)>]
  override x.ToString() = x._Print

  /// [omit]
  [<EditorBrowsableAttribute(EditorBrowsableState.Never)>]
  [<CompilerMessageAttribute("This method is intended for use in generated code only.", 10001, IsHidden=true, IsError=false)>]
  static member Create(element) =
    { XElement = element }
  
  /// [omit]
  [<EditorBrowsableAttribute(EditorBrowsableState.Never)>]
  [<CompilerMessageAttribute("This method is intended for use in generated code only.", 10001, IsHidden=true, IsError=false)>]
  static member Create(reader:TextReader) =    
    use reader = reader
    let text = reader.ReadToEnd()
    let element = XDocument.Parse(text).Root 
    { XElement = element }
  
  /// [omit]
  [<EditorBrowsableAttribute(EditorBrowsableState.Never)>]
  [<CompilerMessageAttribute("This method is intended for use in generated code only.", 10001, IsHidden=true, IsError=false)>]
  static member CreateList(reader:TextReader) = 
    use reader = reader
    let text = reader.ReadToEnd()
    try
      XDocument.Parse(text).Root.Elements()
      |> Seq.map (fun value -> { XElement = value })
      |> Seq.toArray
    with _ when text.TrimStart().StartsWith "<" ->
      XDocument.Parse("<root>" + text + "</root>").Root.Elements()
      |> Seq.map (fun value -> { XElement = value })
      |> Seq.toArray

/// Static helper methods called from the generated code for working with XML
type XmlRuntime = 

  // Operations for getting node values and values of attributes
    
  static member TryGetValue(xml:XmlElement) = 
    if String.IsNullOrEmpty(xml.XElement.Value) then None else Some xml.XElement.Value

  static member TryGetAttribute(xml:XmlElement, nameWithNS) = 
    let attr = xml.XElement.Attribute(XName.Get(nameWithNS))
    if attr = null then None else Some attr.Value

  // Operations that obtain children - depending on the inference, we may
  // want to get an array, option (if it may or may not be there) or 
  // just the value (if we think it is always there)

  static member private GetChildrenArray(value:XmlElement, nameWithNS:string) =
    let namesWithNS = nameWithNS.Split '|'
    let mutable current = value.XElement
    for i = 0 to namesWithNS.Length - 2 do
        if current <> null then
            current <- current.Element(XName.Get namesWithNS.[i])
    let value = current
    if value = null then [| |]
    else [| for c in value.Elements(XName.Get namesWithNS.[namesWithNS.Length - 1]) -> { XElement = c } |]
  
  static member private GetChildOption(value:XmlElement, nameWithNS) =
    match XmlRuntime.GetChildrenArray(value, nameWithNS) with
    | [| it |] -> Some it
    | [| |] -> None
    | array -> failwithf "XML mismatch: Expected zero or one '%s' child, got %d" nameWithNS array.Length

  static member GetChild(value:XmlElement, nameWithNS) =
    match XmlRuntime.GetChildrenArray(value, nameWithNS) with
    | [| it |] -> it
    | array -> failwithf "XML mismatch: Expected exactly one '%s' child, got %d" nameWithNS array.Length

  // Functions that transform specified chidlrens using a transformation
  // function - we need a version for array and option
  // (This is used e.g. when transforming `<a>1</a><a>2</a>` to `int[]`)

  static member ConvertArray<'R>(xml:XmlElement, nameWithNS, f:Func<XmlElement,'R>) : 'R[] = 
    XmlRuntime.GetChildrenArray(xml, nameWithNS) |> Array.map f.Invoke

  static member ConvertOptional<'R>(xml:XmlElement, nameWithNS, f:Func<XmlElement,'R>) =
    XmlRuntime.GetChildOption(xml, nameWithNS) |> Option.map f.Invoke

  static member ConvertOptional2<'R>(xml:XmlElement, nameWithNS, f:Func<XmlElement,'R option>) =
    XmlRuntime.GetChildOption(xml, nameWithNS) |> Option.bind f.Invoke

  /// Returns Some if the specified XmlElement has the specified name
  /// (otherwise None is returned). This is used when the current element
  /// can be one of multiple elements.
  static member ConvertAsName<'R>(xml:XmlElement, nameWithNS, f:Func<XmlElement,'R>) = 
    if xml.XElement.Name = XName.Get(nameWithNS) then Some(f.Invoke xml)
    else None

  /// Returns the contents of the element as a JsonValue
  static member GetJsonValue(xml, cultureStr) = 
    match XmlRuntime.TryGetValue(xml) with
    | Some jsonStr -> JsonDocument.Create(new StringReader(jsonStr), cultureStr)
    | None -> failwithf "XML mismatch: Element doesn't contain value: %A" xml

  /// Tries to return the contents of the element as a JsonValue
  static member TryGetJsonValue(xml, cultureStr) = 
    match XmlRuntime.TryGetValue(xml) with
    | Some jsonStr -> 
        try
            JsonDocument.Create(new StringReader(jsonStr), cultureStr) |> Some
        with _ -> None
    | None -> None

  /// Creates a XElement with a scalar value and wraps it in a XmlElement
  static member CreateValue(nameWithNS, value:obj, cultureStr) = 
    XmlRuntime.CreateRecord(nameWithNS, [| |], [| "", value |], cultureStr)

  // Creates a XElement with the given attributes and elements and wraps it in a XmlElement
  static member CreateRecord(nameWithNS, attributes:_[], elements:_[], cultureStr) =
    let cultureInfo = TextRuntime.GetCulture cultureStr
    let toXmlContent (v:obj) = 
        let inline strWithCulture v =
            (^a : (member ToString : IFormatProvider -> string) (v, cultureInfo)) 
        let serialize (v:obj) =
            match v with
            | :? XmlElement as v -> box v.XElement
            | _ ->
                match v with
                | :? string        as v -> v
                | :? DateTime      as v -> strWithCulture v
                | :? int           as v -> strWithCulture v
                | :? int64         as v -> strWithCulture v
                | :? float         as v -> strWithCulture v
                | :? decimal       as v -> strWithCulture v
                | :? bool          as v -> if v then "true" else "false"
                | :? Guid          as v -> v.ToString()
                | :? IJsonDocument as v -> v.JsonValue.ToString()
                | _ -> failwithf "Unexpected value: %A" v
                |> box
        let inline optionToArray f = function Some x -> [| f x |] | None -> [| |]
        match v with
        | :? Array as v -> [| for elem in v -> serialize elem |]
        | :? option<XmlElement>    as v -> optionToArray serialize v
        | :? option<string>        as v -> optionToArray serialize v
        | :? option<DateTime>      as v -> optionToArray serialize v
        | :? option<int>           as v -> optionToArray serialize v
        | :? option<int64>         as v -> optionToArray serialize v
        | :? option<float>         as v -> optionToArray serialize v
        | :? option<decimal>       as v -> optionToArray serialize v
        | :? option<bool>          as v -> optionToArray serialize v
        | :? option<Guid>          as v -> optionToArray serialize v
        | :? option<IJsonDocument> as v -> optionToArray serialize v
        | v -> [| box (serialize v) |]
    let createElement (parent:XElement) (nameWithNS:string) =
        let namesWithNS = nameWithNS.Split '|'
        (parent, namesWithNS)
        ||> Array.fold (fun parent nameWithNS ->
            let xname = XName.Get nameWithNS
            if parent = null then
                XElement xname
            else
                let element = if nameWithNS = Seq.last namesWithNS
                              then null 
                              else parent.Element(xname) 
                if element = null then
                    let element = XElement xname
                    parent.Add element
                    element
                else
                    element)
    let element = createElement null nameWithNS
    for nameWithNS, value in attributes do
        let xname = XName.Get nameWithNS
        match toXmlContent value with
        | [| |] -> ()
        | [| v |] when v :? string && element.Attribute(xname) = null -> element.SetAttributeValue(xname, v)
        | _ -> failwithf "Unexpected attribute value: %A" value
    for nameWithNS, value in elements do
        if nameWithNS = "" then // it's the value
            match toXmlContent value with
            | [| |] -> ()
            | [| v |] when v :? string && element.Value = "" -> element.Add v
            | _ -> failwithf "Unexpected content value: %A" value
        else
            for value in toXmlContent value do
                match value with
                | :? XElement as v -> 
                    let parentNames = nameWithNS.Split('|') |> Array.rev
                    if v.Name.ToString() <> parentNames.[0] then
                        failwithf "Unexpected element: %O" v
                    let v = 
                        (v, Seq.skip 1 parentNames)
                        ||> Seq.fold (fun element nameWithNS -> 
                            if element.Parent = null then 
                                let parent = createElement null nameWithNS 
                                parent.Add element
                                parent
                            else 
                                if element.Parent.Name.ToString() <> nameWithNS then
                                    failwithf "Unexpected element: %O" v
                                element.Parent)
                    element.Add v
                | :? string as v -> 
                    let child = createElement element nameWithNS 
                    child.Value <- v
                | _ -> failwithf "Unexpected content for child %s: %A" nameWithNS value
    XmlElement.Create element
