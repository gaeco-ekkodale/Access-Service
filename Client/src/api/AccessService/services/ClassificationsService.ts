// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { ClassificationPropertyDTO } from '../models/ClassificationPropertyDTO';
import type { ClassificationsListSet } from '../models/ClassificationsListSet';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class ClassificationsService {
    /**
     * Retrieve all classifications.
     * An Endpoint to retrieve all classifications.
     * @returns ClassificationsListSet OK
     * @throws ApiError
     */
    public static getClassifications(): CancelablePromise<ClassificationsListSet> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/Classifications',
            errors: {
                401: `Unauthorized`,
                404: `Not Found`,
                500: `Internal Server Error`,
            },
        });
    }
    /**
     * Retrieve properties of certain classification.
     * An Endpoint to retrieve the Properties of a specific classification.
     * @param classificationId
     * @returns ClassificationPropertyDTO OK
     * @throws ApiError
     */
    public static getPropertiesByClassificationId(
        classificationId: string,
    ): CancelablePromise<Array<ClassificationPropertyDTO>> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/Classifications/classification/{classificationId}/properties',
            path: {
                'classificationId': classificationId,
            },
            errors: {
                400: `Bad Request`,
                401: `Unauthorized`,
                404: `Not Found`,
                500: `Internal Server Error`,
            },
        });
    }
}
