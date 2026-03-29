/// <reference path="../pb_data/types.d.ts" />
migrate(
  (db) => {
    const dao = new Dao(db);
    const collection = dao.findCollectionByNameOrId("pbc_1589536405");

    // update collection data
    collection.name = "Entry";

    return dao.saveCollection(collection);
  },
  (db) => {
    const dao = new Dao(db);
    const collection = dao.findCollectionByNameOrId("pbc_1589536405");

    // update collection data
    collection.name = "EntryModel";

    return dao.saveCollection(collection);
  },
);
