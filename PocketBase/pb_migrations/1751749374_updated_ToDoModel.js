/// <reference path="../pb_data/types.d.ts" />
migrate(
  (db) => {
    const dao = new Dao(db);
    const collection = dao.findCollectionByNameOrId("pbc_1036457598");

    // update collection data
    collection.name = "ToDo";

    return dao.saveCollection(collection);
  },
  (db) => {
    const dao = new Dao(db);
    const collection = dao.findCollectionByNameOrId("pbc_1036457598");

    // update collection data
    collection.name = "ToDoModel";

    return dao.saveCollection(collection);
  },
);
