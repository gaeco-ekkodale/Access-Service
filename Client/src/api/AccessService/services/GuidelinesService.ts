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
import type { ClassificationDetailDTO } from '../models/ClassificationDetailDTO';
import type { ClassificationsListSet } from '../models/ClassificationsListSet';
import type { GuidelineDTO } from '../models/GuidelineDTO';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class GuidelinesService {
    /**
     * Retrieve all guidelines.
     * An Endpoint to retrieve all available guidelines.
     * @returns GuidelineDTO OK
     * @throws ApiError
     */
    public static getGuidelines(): CancelablePromise<Array<GuidelineDTO>> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/Guidelines',
            errors: {
                401: `Unauthorized`,
                404: `Not Found`,
                500: `Internal Server Error`,
            },
        });
    }
    /**
     * Retrieve classifications of a specific guideline.
     * An Endpoint to retrieve all classifications belonging to a specific guideline.
     * @param guidelineId
     * @returns ClassificationsListSet OK
     * @throws ApiError
     */
    public static getClassificationsByGuideline(
        guidelineId: string,
    ): CancelablePromise<ClassificationsListSet> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/Guidelines/{guidelineId}/classifications',
            path: {
                'guidelineId': guidelineId,
            },
            errors: {
                400: `Bad Request`,
                401: `Unauthorized`,
                404: `Not Found`,
                500: `Internal Server Error`,
            },
        });
    }
    /**
     * Retrieve property sets and properties of a classification.
     * An Endpoint to retrieve all property sets (with their properties) and standalone properties of a specific classification within a guideline.
     * @param guidelineId
     * @param classificationId
     * @returns ClassificationDetailDTO OK
     * @throws ApiError
     */
    public static getClassificationDetail(
        guidelineId: string,
        classificationId: string,
    ): CancelablePromise<ClassificationDetailDTO> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/Guidelines/{guidelineId}/classifications/{classificationId}/detail',
            path: {
                'guidelineId': guidelineId,
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
