import { Stack, Flex, Text, Divider, Box, Button, Spacer } from '@chakra-ui/react';
import { ColorModeSwitcher } from '../ColorModeSwitcher';

interface InputProps {
    hidden?: boolean;
    children?: JSX.Element | JSX.Element[];
    userDetails?: UserDetails;
    onLogoutClick(): void;
}

export interface UserDetails {
    email: string;
    username: string;
}

export function MainUI(props: InputProps) {
    if (props.hidden === undefined || !props.hidden) {
        return (
            <Stack spacing={3} padding={3} hidden={false}>
                <Flex>
                    <Text>{props.userDetails?.username}</Text>
                    <Spacer />
                    <Box>
                        <ColorModeSwitcher />
                        <Button onClick={props.onLogoutClick}>Logout</Button>
                    </Box>
                </Flex>
                <Divider />
                <Box width="100%">{props.children}</Box>
            </Stack>
        );
    } else {
        return null;
    }
}

export default MainUI;
