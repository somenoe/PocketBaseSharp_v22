/// <reference path="../pb_data/types.d.ts" />
migrate(
  (db) => {
    const dao = new Dao(db);
    const collection = dao.findCollectionByNameOrId("pbc_1589536405");

    // add field
    if (
      !collection.schema
        .fields()
        .some((field) => field && field.name && field.name.toLowerCase() === "todo_id")
    ) {
      collection.schema.addField(
        new SchemaField({
          cascadeDelete: true,
          collectionId: "pbc_1036457598",
          hidden: false,
          id: "relation2955387149",
          maxSelect: 1,
          minSelect: 0,
          name: "Todo_Id",
          presentable: false,
          required: false,
          system: false,
          type: "relation",
        }),
      );
    }

    return dao.saveCollection(collection);
  },
  (db) => {
    const dao = new Dao(db);
    const collection = dao.findCollectionByNameOrId("pbc_1589536405");

    // remove field
    collection.schema.removeField("relation2955387149");

    return dao.saveCollection(collection);
  },
);
