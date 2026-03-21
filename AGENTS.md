# AGENTS.md

This file provides guidance to ai agents when working with code in this repository.

## Overview

**tonl-net** is a .NET library implementing TONL (Token-Optimized Notation Language).

## Architecture

Use framework version with latest supported language features as specified in `global.json`

*`src/Tonl.Net/` — the library. Public API entry point is `TonlDocument`.
* `tests/Tonl.Net.Tests/` — NUnit 4 test project using Microsoft Testing Platform runner.

## General Instructions

* use English for all code, comments and docs
* all public members are to be documented
* make only high-confidence changes, avoid speculative changes
* maintainability is a top priority, adding comments sparingly as support as why something is done the way it is
* when stuck: ask clarifying questions or propose a short plan

## Code Style
* follow coding conventions in .editorconfig
* prefer using latest C# features
* prefer `var` when type is obvious from right-hand side, but use explicit type when not
* use target-type `new` and collection expressions
* use `this` keyword only when needed to avoid ambiguity
* trust type null annotations and don't check for nulls when non-nullability guaranteed
* ** Important conventions**:
  - `_camelCase` private fields
  - `camelCase()` private methods
  - `_camelCase()` private functions

## Testing
* Test classes follow the `<Subject>Tester` naming convention
* `using Subject = <SystemUnderTestType>` aliases
* Test methods follow the _MethodName_StateUnderTest_ExpectedBehavior_ pattern
* follow Arrange-Act-Assert
* support types are to be placed in sub-folder named `Support`
* write test firsts when asked to do so

## Build and Test Commands

* **Build:** `dotnet build` (from repository root)
* **Test All:** `dotnet test` (from repository root)
* **Run Single Test:** `dotnet test --filter "FQN~YourNamespace.YourClass.YourMethod"`
