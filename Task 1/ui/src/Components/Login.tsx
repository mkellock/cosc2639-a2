import { Stack, Input, Button, Link, Alert, AlertIcon } from '@chakra-ui/react';
import { useState } from 'react';

export interface LoginDetails {
    email: string;
    password: string;
}

export interface InputProps {
    hidden?: boolean;
    inError?: boolean;
    errorMessage?: string;
    onSubmit(props: LoginDetails): void;
    onRegister(): void;
}

export function Login(props: InputProps) {
    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [inError, setInError] = useState<boolean>();
    const [errorMessage, setErrorMessage] = useState<string>();

    const submit = () => {
        setInError(false);
        setErrorMessage('');

        if (email.length === 0 || password.length === 0) {
            setInError(true);
            setErrorMessage('Please enter your email address and password');
        } else {
            const loginDetails: LoginDetails = {
                email: email,
                password: password,
            };

            props.onSubmit(loginDetails);
        }
    };

    if (props.hidden === undefined || !props.hidden) {
        return (
            <>
                {inError || props.inError ? (
                    <Alert status="error">
                        <AlertIcon />
                        {errorMessage}
                        {props.errorMessage}
                    </Alert>
                ) : null}
                <Stack spacing={3} width={450} padding={3} hidden={false}>
                    <Input onChange={(e) => setEmail(e.target.value)} placeholder="EMail" />
                    <Input onChange={(e) => setPassword(e.target.value)} placeholder="Password" type="password" />
                    <Button borderRadius={15} onClick={submit}>
                        Login
                    </Button>
                    <Link onClick={props.onRegister}>Register</Link>
                </Stack>
            </>
        );
    } else {
        return null;
    }
}

export default Login;
