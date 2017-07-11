#csDelaunay
==========

## Introduction

This is a refactoring of [PouletFrit's C# port](https://github.com/PouletFrit/csDelaunay)
of [as3delaunay](http://nodename.github.io/as3delaunay/).

## Changes

I've added the capability to recalculate diagrams "in-place" without discarding
the site objects and essentially creating a completely new diagram every time.
The diagram also does not immediately ditch the vertices after computing.

By guaranteeing the sites are constant however, means that any specific edge or
vertex cannot be guaranteed to exist.

There were also a large number of style edits for clarity, consistency,
and explicitness, as well as typo and punctuation edits on comments. 

I made these changes because I needed a Voronoi diagram that is editable.

## Getting Started

The library should largely work like the original, but there were some minor
changes to the interfaces of various classes. As such, if you're looking to
retain the exact interface of the original library, it would be best not to use
this version.

The diagram class `Voronoi` now also exposes a `Sites` property which gives a
`List<Vector2f>`. This allows you to retrieve and edit the sites of the diagram
directly. You then use the `Update()` method on a `Voronoi` object to
recalculate all the edges and vertices around the updated site locations.
