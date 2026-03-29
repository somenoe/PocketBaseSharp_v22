/// <reference path="../pb_data/types.d.ts" />
migrate(
  (db) => {
    const dao = new Dao(db);
    const collection = dao.findCollectionByNameOrId("pbc_1036457598");

    // update collection data
    collection.createRule = "";
    collection.deleteRule = "";
    collection.listRule = "";
    collection.updateRule = "";
    collection.viewRule = "";

    return dao.saveCollection(collection);
  },
  (db) => {
    const dao = new Dao(db);
    const collection = dao.findCollectionByNameOrId("pbc_1036457598");

    // update collection data
    collection.createRule = null;
    collection.deleteRule = null;
    collection.listRule = null;
    collection.updateRule = null;
    collection.viewRule = null;

    return dao.saveCollection(collection);
  },
);
