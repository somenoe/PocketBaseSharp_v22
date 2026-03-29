/// <reference path="../pb_data/types.d.ts" />
migrate(
  (db) => {
    const dao = new Dao(db);
    const collection = dao.findCollectionByNameOrId("pbc_1036457598");

    // update field
    if (
      !collection.schema
        .fields()
        .some((field) => field && field.name && field.name.toLowerCase() === "name")
    ) {
      collection.schema.addField(
        new SchemaField({
          autogeneratePattern: "",
          hidden: false,
          id: "text4262580536",
          max: 0,
          min: 0,
          name: "name",
          pattern: "",
          presentable: false,
          primaryKey: false,
          required: false,
          system: false,
          type: "text",
        }),
      );
    }

    return dao.saveCollection(collection);
  },
  (db) => {
    const dao = new Dao(db);
    const collection = dao.findCollectionByNameOrId("pbc_1036457598");

    // update field
    if (
      !collection.schema
        .fields()
        .some((field) => field && field.name && field.name.toLowerCase() === "name")
    ) {
      collection.schema.addField(
        new SchemaField({
          autogeneratePattern: "",
          hidden: false,
          id: "text4262580536",
          max: 0,
          min: 0,
          name: "Name",
          pattern: "",
          presentable: false,
          primaryKey: false,
          required: false,
          system: false,
          type: "text",
        }),
      );
    }

    return dao.saveCollection(collection);
  },
);
