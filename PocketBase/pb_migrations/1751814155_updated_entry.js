/// <reference path="../pb_data/types.d.ts" />
migrate(
  (db) => {
    const dao = new Dao(db);
    const collection = dao.findCollectionByNameOrId("pbc_1589536405");

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

    // update field
    if (
      !collection.schema
        .fields()
        .some((field) => field && field.name && field.name.toLowerCase() === "is_done")
    ) {
      collection.schema.addField(
        new SchemaField({
          hidden: false,
          id: "bool3065835665",
          name: "is_done",
          presentable: false,
          required: false,
          system: false,
          type: "bool",
        }),
      );
    }

    // update field
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
          name: "todo_id",
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

    // update field
    if (
      !collection.schema
        .fields()
        .some((field) => field && field.name && field.name.toLowerCase() === "isdone")
    ) {
      collection.schema.addField(
        new SchemaField({
          hidden: false,
          id: "bool3065835665",
          name: "IsDone",
          presentable: false,
          required: false,
          system: false,
          type: "bool",
        }),
      );
    }

    // update field
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
);
