// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { Box, CircularProgress } from '@mui/material';

/**
 * Component that displays a loading indicator while classifications are being fetched.
 */
const LoadingPage = () => (
    <div className='flex h-full items-center justify-center'>
        <Box sx={{ display: 'flex' }}>
			<CircularProgress />
		</Box>
    </div>
);

export default LoadingPage;
